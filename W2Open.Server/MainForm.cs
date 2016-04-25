using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;
using W2Open.Common;
using W2Open.Common.OutgoingPacketStructure;
using W2Open.Common.Utility;
using W2Open.GameState;
using W2Open.GameState.Plugin;

namespace W2Open.Server
{
    public partial class MainForm : Form
    {
        private CGameStateController gameController;

        public MainForm()
        {
            InitializeComponent();

            PluginController.InstallPlugins();

            W2Log.DidLog += CLog_DidLog;
            
            gameController = new CGameStateController(this);

            StartServer_Channel1();
        }

        private void CLog_DidLog(String txt, ELogType logType)
        {
            Color logColor;

            switch (logType)
            {
                case ELogType.CRITICAL_ERROR:
                    logColor = Color.Red;
                    break;

                case ELogType.GAME_EVENT:
                    logColor = Color.Cyan;
                    break;

                case ELogType.NETWORK:
                    logColor = Color.Azure;
                    break;

                default:
                    logColor = Color.GreenYellow;
                    break;
            }

            rtbMainLog.SelectionColor = Color.Yellow;
            rtbMainLog.AppendText(String.Format("[{0:D02}:{1:D02}:{2:D02}] ", DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second));

            rtbMainLog.SelectionColor = logColor;
            rtbMainLog.AppendText(txt + "\n");
        }

        /// <summary>
        /// Process the newly connected client.
        /// </summary>
        /// <param name="client">The newly connected client.</param>
        private async void ProcessClient_Channel1(TcpClient client)
        {
            using (client)
            using (NetworkStream stream = client.GetStream())
            {
                CPlayer player = new CPlayer(stream);
                CCompoundBuffer packet = player.RecvPacket;

                try
                {
                    // Iterate processing incoming player packets until he disconnects.
                    while (player.State != EPlayerState.CLOSED)
                    {
                        int readCount = await stream.ReadAsync(packet.RawBuffer, 0, NetworkBasics.MAXL_PACKET);

                        if (readCount != 4 && (readCount < 12 || readCount > NetworkBasics.MAXL_PACKET)) // Invalid game packet.
                        {
                            gameController.DisconnectPlayer(player);
                            break;
                        }
                        else // Possible valid game packet chunk.
                        {
                            unsafe
                            {
                                packet.Offset = 0;
                                fixed (byte* PinnedPacketChunk = &packet.RawBuffer[packet.Offset])
                                {
                                    // Check for the init code.
                                    if (*(uint*)&PinnedPacketChunk[packet.Offset] == NetworkBasics.INIT_CODE)
                                    {
                                        packet.Offset = 4;

                                        // If a valid index can't be assigned to the player, disconnect him 
                                        if (!gameController.TryInsertPlayer(player))
                                        {
                                            player.SendPacket(MTextMessagePacket.Create("O servidor está lotado. Tente novamente mais tarde."));
                                            gameController.DisconnectPlayer(player);
                                            continue;
                                        }

                                        // If all the received chunk resumes to the INIT_CODE, read the next packet.
                                        if (readCount == 4)
                                            continue;
                                    }

                                    // Process all possible packets that were possibly sent together.
                                    while (packet.Offset < readCount && player.State != EPlayerState.CLOSED)
                                    {
                                        // Check if the game packet size is bigger than the remaining received chunk.
                                        if (packet.ReadNextUShort(0) > readCount - packet.Offset || packet.ReadNextUShort(0) < 12)
                                            throw new Exception("Invalid received packet. Reading packet is bigger than the remaining chunk.");

                                        // Tries to decrypt the packet.
                                        if (!PacketSecurity.Decrypt(packet))
                                            throw new Exception($"Can't decrypt a packet received from {client.Client.RemoteEndPoint}.");

                                        W2Log.Write(String.Format("Processing recv packet {{0x{0:X}/{1}}} from {2}.",
                                            packet.ReadNextUShort(4), packet.ReadNextUShort(0), client.Client.RemoteEndPoint), ELogType.NETWORK);

                                        // Process the incoming packet.
                                        EPlayerRequestResult requestResult = gameController.ProcessPlayerRequest(player);

                                        // Treat the processing packet return.
                                        switch (requestResult)
                                        {
                                            case EPlayerRequestResult.PACKET_NOT_HANDLED:
                                            {
                                                W2Log.Write(String.Format("Recv packet {{0x{0:X}/{1}}} from {2} didn't was processed.",
                                                    packet.ReadNextUShort(4), packet.ReadNextUShort(0), client.Client.RemoteEndPoint), ELogType.NETWORK);

                                                byte[] rawPacket = new byte[packet.ReadNextUShort(0)];
                                                for (int i = 0; i < rawPacket.Length; i++)
                                                    rawPacket[i] = PinnedPacketChunk[i + packet.Offset];

                                                File.WriteAllBytes($@"..\..\Dumped Packets\Inner Packets\{packet.ReadNextUShort(4)}.bin",
                                                    rawPacket);
                                                break;
                                            }

                                            case EPlayerRequestResult.CHECKSUM_FAIL:
                                            {
                                                W2Log.Write($"Recv packet from {client.Client.RemoteEndPoint} have invalid checksum.", ELogType.CRITICAL_ERROR);
                                                gameController.DisconnectPlayer(player);
                                                break;
                                            }

                                            case EPlayerRequestResult.PLAYER_INCONSISTENT_STATE:
                                            {
                                                W2Log.Write($"A player was disconnected due to inconsistent PlayerState.", ELogType.CRITICAL_ERROR);
                                                gameController.DisconnectPlayer(player);
                                                break;
                                            }
                                        }

                                        // Correct the offset to process the next packet in the received chunk.
                                        player.RecvPacket.Offset += packet.ReadNextUShort(0);
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    W2Log.Write($"An unhandled exception happened processing the player {player.Index}. MSG: {ex.Message}", ELogType.CRITICAL_ERROR);
                }
                finally
                {
                    gameController.DisconnectPlayer(player);
                }
            }
        }

        /// <summary>
        /// Start listenning the server connection.
        /// </summary>
        private async void StartServer_Channel1()
        {
            TcpListener listener = new TcpListener(IPAddress.Parse("192.168.1.69"), 8281);

            listener.Start();

            W2Log.Write($"The Game Server is listenning at {listener.Server.LocalEndPoint}.", ELogType.NETWORK);

            try
            {
                while(true)
                {
                    TcpClient thisClient = await listener.AcceptTcpClientAsync();

                    W2Log.Write($"New client connected {thisClient.Client.RemoteEndPoint}.", ELogType.NETWORK);

                    ProcessClient_Channel1(thisClient);
                }
            }
            finally
            {
                listener.Stop();
            }
        }
    }
}
