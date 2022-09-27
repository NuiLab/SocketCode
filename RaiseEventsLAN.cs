/*
 * Author: Dan Rehberg
 * Purpose: LAN and Remote server control to replace use of PhotonEngine
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ExitGames.Client.Photon;
using Photon.Pun;
using System;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using Photon_IATK;
using System.Text;

public class RaiseEventsLAN// : MonoBehaviour
{
    //Return sends
    static public Action<byte, object> OnEventLAN;

    //Socket Stuff
    static bool connected = false;
    static TcpClient cSocket;
    static readonly String ServerIP = "10.0.0.41";
    static readonly Int32 port = 40666;
    static readonly Int32 MaxBuffer = 262144;
    static byte[] message = new byte[MaxBuffer];
    static byte[] recMessage = new byte[MaxBuffer];
    static NetworkStream stream;

    static private UInt32 EventBytes(object obj, byte eventCode)
    {
        UInt32 len = 0;
        object[] oArray = (object[])obj;
        int index = 6;
        switch (eventCode)
        {
            //Annotation case
            case GlobalVariables.RequestEventAnnotationContent:
                //int[0]
                break;
            case GlobalVariables.RespondEventWithContent:
                //int[0]
                //string[1]
                break;
            case GlobalVariables.RequestAddPointEvent:
                //int[0]
                //string
                break;
            case GlobalVariables.RequestTextUpdate:
                //int[0]
                //string[1]
                break;
            case GlobalVariables.PhotonRequestAnnotationsDeleteOneEvent:
                //int[0]
                break;
            case GlobalVariables.RequestCentralityUpdate:
                //int[0]
                //string[1]//need a length component to make this work safely
                //string[2]
                break;
            case GlobalVariables.RequestLineCompleation:
                //int[0]
                break;

            //AnnotationManagerSaveLoadEvents
            case GlobalVariables.RequestEventAnnotationCreation:
                //int[0]
                //byte_array[1]
                break;

            case GlobalVariables.RequestEventAnnotationRemoval:
                //int[0]
                break;
            case GlobalVariables.RequestEventAnnotationFileSystemDeletion:
                //int[0]
                break;
            case GlobalVariables.SendEventNewAnnotationID:
                //int[0]
                //int[1]
                break;
            case GlobalVariables.RequestSaveAnnotation:
                //int[0]
                //string[1]
                break;

            //AnnotationSysnc
            case GlobalVariables.PhotonMoveEvent:
                //int[0]
                //Vec3[1]
                //Quat[2]
                //Vec3[3]
                //int[4]
                break;

            //ChatClient
            //Not doing anything with chat client because the serialization is more complicated

            //GeneralEventManager
            case GlobalVariables.PhotonVisSceneInstantiateEvent:
                //int
                len = 4;// + 4 * 3;
                Array.Copy(BitConverter.GetBytes((int)oArray[0]), 0, message, index, 4);
                index += sizeof(int);//4;
                break;
            case GlobalVariables.PhotonDeleteAllObjectsWithComponentEvent:
                //int
                len = 4;// + 4 * 3;
                Array.Copy(BitConverter.GetBytes((int)oArray[0]), 0, message, index, 4);
                index += sizeof(int);//4;
                //string
                break;
            case GlobalVariables.PhotonDeleteSingleObjectsWithViewIDEvent:
                //int
                len = 4;// + 4 * 3;
                Array.Copy(BitConverter.GetBytes((int)oArray[0]), 0, message, index, 4);
                index += sizeof(int);//4;
                //int
                len += 4;// + 4 * 3;
                Array.Copy(BitConverter.GetBytes((int)oArray[1]), 0, message, index, 4);
                index += sizeof(int);//4;
                break;
            case GlobalVariables.PhotonRequestLatencyCheckEvent:
                //int
                len = 4;// + 4 * 3;
                Array.Copy(BitConverter.GetBytes((int)oArray[0]), 0, message, index, 4);
                index += sizeof(int);//4;
                //int
                len += 4;// + 4 * 3;
                Array.Copy(BitConverter.GetBytes((int)oArray[1]), 0, message, index, 4);
                index += sizeof(int);//4;
                //string need size
                string temp = (string)oArray[2];
                len += 4;// + 4 * 3;
                Array.Copy(BitConverter.GetBytes(temp.Length), 0, message, index, 4);
                index += sizeof(int);//4;
                Array.Copy(Encoding.ASCII.GetBytes(temp), 0, message, index, temp.Length);
                //int
                len += 4;// + 4 * 3;
                Array.Copy(BitConverter.GetBytes((int)oArray[3]), 0, message, index, 4);
                index += sizeof(int);//4;
                break;
            case GlobalVariables.PhotonRequestLatencyCheckResponseEvent:
                //int
                len = 4;// + 4 * 3;
                Array.Copy(BitConverter.GetBytes((int)oArray[0]), 0, message, index, 4);
                index += sizeof(int);//4;
                //int
                //string
                //string
                //int
                //string
                //int
                break;
            /*case GlobalVariables.RequestElicitationSetupEvent:

                break;*///This is using RPC instead

            //GenericTransformSync
            //case GlobalVariables.PhotonMoveEvent://repeated, already defined above
            case GlobalVariables.PhotonRequestTransformEvent:
                //int
                len = 4;// + 4 * 3;
                Array.Copy(BitConverter.GetBytes((int)oArray[0]), 0, message, index, 4);
                index += sizeof(int);//4;
                //int
                break;
            case GlobalVariables.PhotonRespondToRequestTransformEvent:
                //int
                len = 4;// + 4 * 3;
                Array.Copy(BitConverter.GetBytes((int)oArray[0]), 0, message, index, 4);
                index += sizeof(int);//4;
                //int
                //Vec3
                //Quat
                //Vec3
                break;

            //GenericTransformSyncLerp
            //case GlobalVariables.PhotonMoveEvent;//repeat
            //remaining two cases are also repeats from generictranformsync

            //GrabFeedback
            case GlobalVariables.RequestGrabEvent:
                //int
                len = 4;// + 4 * 3;
                Array.Copy(BitConverter.GetBytes((int)oArray[0]), 0, message, index, 4);
                index += sizeof(int);//4;
                break;
            case GlobalVariables.RequestReleaseEvent:
                len = 4;// + 4 * 3;
                Array.Copy(BitConverter.GetBytes((int)oArray[0]), 0, message, index, 4);
                index += sizeof(int);//4;
                /*Vector3 test = (Vector3)oArray[1];
                Array.Copy(BitConverter.GetBytes(test.x), 0, message, index, 4);
                index += sizeof(float);//4;
                Array.Copy(BitConverter.GetBytes(test.y), 0, message, index, 4);
                index += sizeof(float);//4;
                Array.Copy(BitConverter.GetBytes(test.z), 0, message, index, 4);
                index += sizeof(float);//4;*/
                break;
            case GlobalVariables.RequestGrabHandleEvent:
                //int
                len = 4;// + 4 * 3;
                Array.Copy(BitConverter.GetBytes((int)oArray[0]), 0, message, index, 4);
                index += sizeof(int);//4;
                break;
            case GlobalVariables.RequestReleaseHandleEvent:
                //int
                len = 4;// + 4 * 3;
                Array.Copy(BitConverter.GetBytes((int)oArray[0]), 0, message, index, 4);
                index += sizeof(int);//4;
                break;

            //MovePlayspaceInterface
            case GlobalVariables.RequestPlayspaceTransform:
                //int
                len = 4;// + 4 * 3;
                Array.Copy(BitConverter.GetBytes((int)oArray[0]), 0, message, index, 4);
                index += sizeof(int);//4;
                //int
                break;
            case GlobalVariables.SendPlayspaceTransform:
                //int
                len = 4;// + 4 * 3;
                Array.Copy(BitConverter.GetBytes((int)oArray[0]), 0, message, index, 4);
                index += sizeof(int);//4;
                //Vec3
                //Quat
                //Int
                break;
            case GlobalVariables.RequestUpdatePlayspaceTransform:
                //int
                len = 4;// + 4 * 3;
                Array.Copy(BitConverter.GetBytes((int)oArray[0]), 0, message, index, 4);
                index += sizeof(int);//4;
                //Vec3
                //Quat
                //int
                break;
            case GlobalVariables.RequestHideTrackers:
                //int
                len = 4;// + 4 * 3;
                Array.Copy(BitConverter.GetBytes((int)oArray[0]), 0, message, index, 4);
                index += sizeof(int);//4;
                //int
                break;

            //PhotonPlayer
            /*case GlobalVariables.PhotonRequestHideControllerModelsEvent:

                break;
            case GlobalVariables.PhotonRequestNicknameUpdateEvent:

                break;
            case GlobalVariables.PhotonRequestHideExtrasEvent:

                break;*/

            //UnParentReparent
            case GlobalVariables.RequestParentEvent:
                {
                    //int
                    len = 4;// + 4 * 3;
                    Array.Copy(BitConverter.GetBytes((int)oArray[0]), 0, message, index, 4);
                    index += sizeof(int);//4;
                                         //Vec3
                    Vector3 test = (Vector3)oArray[1];
                    Array.Copy(BitConverter.GetBytes(test.x), 0, message, index, 4);
                    index += sizeof(float);//4;
                    Array.Copy(BitConverter.GetBytes(test.y), 0, message, index, 4);
                    index += sizeof(float);//4;
                    Array.Copy(BitConverter.GetBytes(test.z), 0, message, index, 4);
                    index += sizeof(float);//4;
                                           //Quat
                                           //Vec3
                                           //int
                    break;
                }
            case GlobalVariables.RequestUnParentEvent:
                {
                    //int
                    len = 4;// + 4 * 3;
                    Array.Copy(BitConverter.GetBytes((int)oArray[0]), 0, message, index, 4);
                    index += sizeof(int);//4;
                                         //Vec3
                    Vector3 test = (Vector3)oArray[1];
                    Array.Copy(BitConverter.GetBytes(test.x), 0, message, index, 4);
                    index += sizeof(float);//4;
                    Array.Copy(BitConverter.GetBytes(test.y), 0, message, index, 4);
                    index += sizeof(float);//4;
                    Array.Copy(BitConverter.GetBytes(test.z), 0, message, index, 4);
                    index += sizeof(float);//4;
                                           //Quat
                                           //Vec3
                                           //int
                    break;
                }

            //VisualizationEvent_Calls
            //This is just kind of long and is being skipped until the others are setup
            /*case GlobalVariables.PhotonChangeX_AxisEvent:

                break;
            case GlobalVariables.PhotonChangeY_AxisEvent:

                break;
            case GlobalVariables.PhotonChangeZ_AxisEvent:

                break;
            case GlobalVariables.PhotonChangeColorDimensionEvent:

                break;
            case GlobalVariables.PhotonChangeSizeDimensionEvent:

                break;
            case GlobalVariables.PhotonRequestStateEvent:

                break;
            case GlobalVariables.PhotonRequestStateEventResponse:

                break;*/
        }
        return len;
    }
    static private object DecipherBytes()
    {
        object nothing = new object();
        int index = 6;
        switch (recMessage[4])
        {
            //GrabFeedback
            case GlobalVariables.RequestGrabEvent:
                {
                    int viewID = BitConverter.ToInt32(recMessage, index);
                    index += sizeof(int);// 4;
                    object[] temp = new object[] { viewID };//, test};
                    return temp;
                }
            case GlobalVariables.RequestReleaseEvent:
                {
                    int viewID = BitConverter.ToInt32(recMessage, index);
                    index += sizeof(int);// 4;
                    /*Vector3 test;
                    test.x = BitConverter.ToSingle(recMessage, index);
                    index += sizeof(float);// 4;
                    test.y = BitConverter.ToSingle(recMessage, index);
                    index += sizeof(float);//4;
                    test.z = BitConverter.ToSingle(recMessage, index);
                    index += sizeof(float);//4;*/
                    object[] temp = new object[] { viewID };//, test};
                    return temp;
                }
            case GlobalVariables.RequestGrabHandleEvent:
                {
                    int viewID = BitConverter.ToInt32(recMessage, index);
                    index += sizeof(int);// 4;
                    object[] temp = new object[] { viewID };//, test};
                    return temp;
                }
            case GlobalVariables.RequestReleaseHandleEvent:
                {
                    int viewID = BitConverter.ToInt32(recMessage, index);
                    index += sizeof(int);// 4;
                    object[] temp = new object[] { viewID };//, test};
                    return temp;
                }
        }
        return nothing;
    }

    static public void init()
    {
        PhotonNetwork.reLAN = sendOut;
        //Connect to socket
        try
        {
            //Create client, should fail if server non-existent
            cSocket = new TcpClient();// new TcpClient(ServerIP, port);
            cSocket.Connect(ServerIP, port);

            for (int i = 0; i < MaxBuffer; ++i)
            {
                message[i] = Convert.ToByte(((i % 95) + 32));
            }
            message[MaxBuffer - 1] = Convert.ToByte('\0');

            stream = cSocket.GetStream();

            UInt32 len = 6;
            byte[] temp = BitConverter.GetBytes(len);
            message[0] = temp[0];
            message[1] = temp[1];
            message[2] = temp[2];
            message[3] = temp[3];
            message[5] = Convert.ToByte('\0');
            //stream.Write(message, 0, (Int32)len);

            //bytes = stream.Read(receive, 0, receive.Length);
            connected = true;

            cSocket.ReceiveTimeout = 10;//Block for 10 ms, does not block indefinitely
        }
        catch (ArgumentNullException err)
        {

        }
        catch (SocketException err)
        {

        }
    }

    static public void end()
    {
        if (connected)
        {
            //Close the stream and the client socket
            stream.Close();
            cSocket.Close();
        }
    }

    static public void sendOut(EventData e)
    {
        
    }

    static public void sendTest(byte code, object obj, byte recepient)
    {
        /*byte[] json = ObjectToByteArray(obj);
        UInt32 len = (UInt32)json.Length + 6;
        byte[] temp = BitConverter.GetBytes(len);
        message[0] = temp[0];
        message[1] = temp[1];
        message[2] = temp[2];
        message[3] = temp[3];
        message[4] = code;
        message[5] = recepient;
        Array.Copy(json, 0, message, 6, json.Length);*/
        UInt32 len = EventBytes(obj, code) + 6;
        byte[] temp = BitConverter.GetBytes(len);
        message[0] = temp[0];
        message[1] = temp[1];
        message[2] = temp[2];
        message[3] = temp[3];
        message[4] = code;
        message[5] = recepient;
        stream.Write(message, 0, (Int32)len);
    }

    static private int receivingMessage(int initialSize)
    {
        UInt32 totalSize = BitConverter.ToUInt32(recMessage, 0);

        int received = 0;
        int delta = (int)totalSize - initialSize;
        while (delta > 0)
        {

            received = stream.Read(recMessage, initialSize, delta);
            if (received > 0)
            {
                initialSize += received;
                delta = (int)totalSize - initialSize;
                //std::cout << received << " Gathering long message... " << delta << "\n";
                //if (received < bufferChunk)break;
            }
            else if (received < 0) break;
        }
        if (received < 0) return 0;
        //std::cout << "Total Bytes Gathered: " << initialSize << '\n';
        return initialSize;
    }

    static public void recvIn()
    {
        if (connected)
        {
            try
            {
                int len = stream.Read(recMessage, 0, 4);
                if (len > 0) len = receivingMessage(len);
                //object[] obj = (object[])DecipherBytes();
                //Vector3 testing = (Vector3)obj[1];
                //Debug.Log("Data: " + testing.ToString());
                OnEventLAN(recMessage[4], DecipherBytes());
            }
            catch
            {

            }
        }
    }

    // Update is called once per frame
    /*void Update()
    {
        
    }*/
}
