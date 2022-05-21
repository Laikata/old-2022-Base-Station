using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO.Ports;
using System.Threading;


class RingBuffer
{
    byte[] buffer;
    public int tip { get; private set; }
    const int RING_BUFFER_SIZE = 256;

    public RingBuffer()
    {
        buffer = new byte[RING_BUFFER_SIZE];
        tip = 0;
    }

    public byte[] readBytes(int offset, int amount)
    {
        int distance = Mathf.Abs(offset - tip);

        if (distance > RING_BUFFER_SIZE / 2)
        {
            // The buffer has overflowed
            distance = Mathf.Abs(offset - (tip + RING_BUFFER_SIZE - 1));
        }

        if (distance < amount)
        {
            return new byte[0];
        }

        byte[] result = new byte[amount];
        for (int i = 0; i < amount; i++)
        {
            result[i] = buffer[(offset + i) % RING_BUFFER_SIZE];
        }
        return result;
    }

    public void Append(byte data)
    {
        buffer[tip] = data;
        tip = (tip + 1) % RING_BUFFER_SIZE;
    }
}
enum PacketTypes { INIT, GPS, IMU, ENV, INFO }
enum PacketStatus { NotRead, OK, Rejected };

class PacketBase
{
    public int packetSize;
    public int offset;
    public PacketTypes packetType;
    public RingBuffer buffer;
    public PacketStatus status;

    public PacketBase(RingBuffer pbuffer, int poffset)
    {
        packetSize = 0;
        packetType = PacketTypes.INIT;
        offset = poffset;
        buffer = pbuffer;
        status = PacketStatus.NotRead;
    }

    public void Init(List<GPSPacket> gpslist, List<IMUPacket> imulist, List<ENVPacket> envlist)
    {
        // Counter + Size + Protocol Header
        byte[] data = buffer.readBytes(offset, 3);
        if (data.Length != 0)
        {
            offset += 2;

            byte packet_size = data[0];
            byte counter = data[1];
            byte packet_type = data[2];

            if (packet_size == 13 && packet_type == 0x01) { gpslist.Add(new GPSPacket(buffer, offset)); status = PacketStatus.OK; }
            else if (packet_size == 41 && packet_type == 0x02) { imulist.Add(new IMUPacket(buffer, offset)); status = PacketStatus.OK; }
            else if (packet_size == 13 && packet_type == 0x03) { envlist.Add(new ENVPacket(buffer, offset)); status = PacketStatus.OK; }
            else { Debug.Log("Rejected packet: Header/Size didn't match"); status = PacketStatus.Rejected; }
        }
    }

    protected Vector3 UnpackVec(byte[] data, int start)
    {
        Vector3 unpacked_vec = new Vector3();
        for (int i = 0; i < 3; i++)
        {
            if (System.BitConverter.IsLittleEndian) System.Array.Reverse(data, i * 4 + start, 4);
            unpacked_vec[i] = System.BitConverter.ToSingle(data, i * 4 + start);

        }
        return unpacked_vec;
    }

    protected bool CheckCRC32(byte[] data, int length)
    {
        if (System.BitConverter.IsLittleEndian) System.Array.Reverse(data, length - 4, 4);
        if (CRC32.Compute(data, length - 4) == System.BitConverter.ToUInt32(data, length - 4)) return true;
        else return false;
    }
}

class GPSPacket : PacketBase
{
    public Vector3 position;
    

    public GPSPacket(RingBuffer pbuffer, int poffset) : base(pbuffer, poffset)
    {
        position = new Vector3();
        packetType = PacketTypes.GPS;
    }
    public void Read()
    {
        const int size = 17;
        byte[] data = buffer.readBytes(offset, size);
        if (data.Length != 0)
        {
            if (!CheckCRC32(data, size))
            {
                Debug.Log("Rejected package: Checksum mismatch");
                status = PacketStatus.Rejected;
                return;
            }

            position = UnpackVec(data, 1);

            status = PacketStatus.OK;
        }
    }
}

class IMUPacket : PacketBase
{
    public Vector3 mag;
    public Vector3 accel;
    public Vector3 gyro;
    public float hoz;

    public IMUPacket(RingBuffer pbuffer, int poffset) : base(pbuffer, poffset)
    {
        mag = new Vector3();
        accel = new Vector3();
        gyro = new Vector3();
        hoz = 0.0f;
        packetType = PacketTypes.IMU;
    }
    public void Read()
    {
        const int size = 45;
        byte[] data = buffer.readBytes(offset, size);
        if (data.Length != 0)
        {
            if (!CheckCRC32(data, size))
            {
                Debug.Log("Rejected package: Checksum mismatch");
                status = PacketStatus.Rejected;
                return;
            }

            mag = UnpackVec(data, 1);
            accel = UnpackVec(data, 13);
            gyro = UnpackVec(data, 25);

            if (System.BitConverter.IsLittleEndian) System.Array.Reverse(data, 37, 4);
            hoz = System.BitConverter.ToSingle(data, 37);

            status = PacketStatus.OK;
        }
    }

}

class ENVPacket : PacketBase
{
    public float temp;
    public float hum;
    public float pressure;

    public ENVPacket(RingBuffer pbuffer, int poffset) : base(pbuffer, poffset)
    {
        temp = 0.0f;
        hum = 0.0f;
        pressure = 0.0f;
        packetType = PacketTypes.ENV;
    }
    public void Read()
    {
        const int size = 17;
        byte[] data = buffer.readBytes(offset, size);
        if (data.Length != 0)
        {
            if (!CheckCRC32(data, size))
            {
                Debug.Log("Rejected package: Checksum mismatch");
                status = PacketStatus.Rejected;
                return;
            }

            if (System.BitConverter.IsLittleEndian) System.Array.Reverse(data, 1, 4);
            temp = System.BitConverter.ToSingle(data, 1);
            Debug.Log(temp);
            if (System.BitConverter.IsLittleEndian) System.Array.Reverse(data, 5, 4);
            hum = System.BitConverter.ToSingle(data, 5);
            if (System.BitConverter.IsLittleEndian) System.Array.Reverse(data, 9, 4);
            pressure = System.BitConverter.ToSingle(data, 9);

            status = PacketStatus.OK;


        }

    }
}

static class CRC32
{
    static readonly uint[] crc_table = {
        0x0, 0x77073096, 0xee0e612c, 0x990951ba,
        0x76dc419, 0x706af48f, 0xe963a535, 0x9e6495a3,
        0xedb8832, 0x79dcb8a4, 0xe0d5e91e, 0x97d2d988,
        0x9b64c2b, 0x7eb17cbd, 0xe7b82d07, 0x90bf1d91,
        0x1db71064, 0x6ab020f2, 0xf3b97148, 0x84be41de,
        0x1adad47d, 0x6ddde4eb, 0xf4d4b551, 0x83d385c7,
        0x136c9856, 0x646ba8c0, 0xfd62f97a, 0x8a65c9ec,
        0x14015c4f, 0x63066cd9, 0xfa0f3d63, 0x8d080df5,
        0x3b6e20c8, 0x4c69105e, 0xd56041e4, 0xa2677172,
        0x3c03e4d1, 0x4b04d447, 0xd20d85fd, 0xa50ab56b,
        0x35b5a8fa, 0x42b2986c, 0xdbbbc9d6, 0xacbcf940,
        0x32d86ce3, 0x45df5c75, 0xdcd60dcf, 0xabd13d59,
        0x26d930ac, 0x51de003a, 0xc8d75180, 0xbfd06116,
        0x21b4f4b5, 0x56b3c423, 0xcfba9599, 0xb8bda50f,
        0x2802b89e, 0x5f058808, 0xc60cd9b2, 0xb10be924,
        0x2f6f7c87, 0x58684c11, 0xc1611dab, 0xb6662d3d,
        0x76dc4190, 0x1db7106, 0x98d220bc, 0xefd5102a,
        0x71b18589, 0x6b6b51f, 0x9fbfe4a5, 0xe8b8d433,
        0x7807c9a2, 0xf00f934, 0x9609a88e, 0xe10e9818,
        0x7f6a0dbb, 0x86d3d2d, 0x91646c97, 0xe6635c01,
        0x6b6b51f4, 0x1c6c6162, 0x856530d8, 0xf262004e,
        0x6c0695ed, 0x1b01a57b, 0x8208f4c1, 0xf50fc457,
        0x65b0d9c6, 0x12b7e950, 0x8bbeb8ea, 0xfcb9887c,
        0x62dd1ddf, 0x15da2d49, 0x8cd37cf3, 0xfbd44c65,
        0x4db26158, 0x3ab551ce, 0xa3bc0074, 0xd4bb30e2,
        0x4adfa541, 0x3dd895d7, 0xa4d1c46d, 0xd3d6f4fb,
        0x4369e96a, 0x346ed9fc, 0xad678846, 0xda60b8d0,
        0x44042d73, 0x33031de5, 0xaa0a4c5f, 0xdd0d7cc9,
        0x5005713c, 0x270241aa, 0xbe0b1010, 0xc90c2086,
        0x5768b525, 0x206f85b3, 0xb966d409, 0xce61e49f,
        0x5edef90e, 0x29d9c998, 0xb0d09822, 0xc7d7a8b4,
        0x59b33d17, 0x2eb40d81, 0xb7bd5c3b, 0xc0ba6cad,
        0xedb88320, 0x9abfb3b6, 0x3b6e20c, 0x74b1d29a,
        0xead54739, 0x9dd277af, 0x4db2615, 0x73dc1683,
        0xe3630b12, 0x94643b84, 0xd6d6a3e, 0x7a6a5aa8,
        0xe40ecf0b, 0x9309ff9d, 0xa00ae27, 0x7d079eb1,
        0xf00f9344, 0x8708a3d2, 0x1e01f268, 0x6906c2fe,
        0xf762575d, 0x806567cb, 0x196c3671, 0x6e6b06e7,
        0xfed41b76, 0x89d32be0, 0x10da7a5a, 0x67dd4acc,
        0xf9b9df6f, 0x8ebeeff9, 0x17b7be43, 0x60b08ed5,
        0xd6d6a3e8, 0xa1d1937e, 0x38d8c2c4, 0x4fdff252,
        0xd1bb67f1, 0xa6bc5767, 0x3fb506dd, 0x48b2364b,
        0xd80d2bda, 0xaf0a1b4c, 0x36034af6, 0x41047a60,
        0xdf60efc3, 0xa867df55, 0x316e8eef, 0x4669be79,
        0xcb61b38c, 0xbc66831a, 0x256fd2a0, 0x5268e236,
        0xcc0c7795, 0xbb0b4703, 0x220216b9, 0x5505262f,
        0xc5ba3bbe, 0xb2bd0b28, 0x2bb45a92, 0x5cb36a04,
        0xc2d7ffa7, 0xb5d0cf31, 0x2cd99e8b, 0x5bdeae1d,
        0x9b64c2b0, 0xec63f226, 0x756aa39c, 0x26d930a,
        0x9c0906a9, 0xeb0e363f, 0x72076785, 0x5005713,
        0x95bf4a82, 0xe2b87a14, 0x7bb12bae, 0xcb61b38,
        0x92d28e9b, 0xe5d5be0d, 0x7cdcefb7, 0xbdbdf21,
        0x86d3d2d4, 0xf1d4e242, 0x68ddb3f8, 0x1fda836e,
        0x81be16cd, 0xf6b9265b, 0x6fb077e1, 0x18b74777,
        0x88085ae6, 0xff0f6a70, 0x66063bca, 0x11010b5c,
        0x8f659eff, 0xf862ae69, 0x616bffd3, 0x166ccf45,
        0xa00ae278, 0xd70dd2ee, 0x4e048354, 0x3903b3c2,
        0xa7672661, 0xd06016f7, 0x4969474d, 0x3e6e77db,
        0xaed16a4a, 0xd9d65adc, 0x40df0b66, 0x37d83bf0,
        0xa9bcae53, 0xdebb9ec5, 0x47b2cf7f, 0x30b5ffe9,
        0xbdbdf21c, 0xcabac28a, 0x53b39330, 0x24b4a3a6,
        0xbad03605, 0xcdd70693, 0x54de5729, 0x23d967bf,
        0xb3667a2e, 0xc4614ab8, 0x5d681b02, 0x2a6f2b94,
        0xb40bbe37, 0xc30c8ea1, 0x5a05df1b, 0x2d02ef8d
    };
    public static uint Compute(byte[] data, int data_length)
    {
        uint crc32 = 0xFFFFFFFFu;

        for (int i = 0; i < data_length; i++)
        {
            uint lookupIndex = (crc32 ^ data[i]) & 0xff;
            crc32 = (crc32 >> 8) ^ crc_table[lookupIndex];  // CRCTable is an array of 256 32-bit constants
        }

        return ~crc32;
    }
}



public class PacketParser : MonoBehaviour
{
    public TextUi uiText;
    public string PortName;
    ParserThread parserThread;
    // Start is called before the first frame update
    void Start()
    {
        parserThread = new ParserThread(uiText);
        Thread thread = new Thread(new ThreadStart(parserThread.Run));
        thread.Start();
    }

    private void Update()
    {
        //TODO: Communicate with thread
        if (parserThread.updatedGps)
        {
            lock(parserThread.gpsLock)
            {
                uiText.updatePosition(parserThread.gps);
            }
        }

        if (parserThread.updatedImu)
        {
            lock (parserThread.imuLock)
            {
                uiText.updateMag(parserThread.imu[0], parserThread.imu[1], parserThread.imu[2]);
            }
        }

        if (parserThread.updatedEnv)
        {
            lock (parserThread.envLock)
            {
                uiText.updateEnv(parserThread.env[0], parserThread.env[1], parserThread.env[2]);
            }
        }
    }

    void OnDisable()
    {
        parserThread.Stop();
    }
}

public class ParserThread
{
    SerialPort serialPort;
    List<PacketBase> initPackets;
    List<GPSPacket> gpsPackets;
    List<IMUPacket> imuPackets;
    List<ENVPacket> envPackets;
    RingBuffer buffer;
    TextUi textUI;

    public object gpsLock = new object();
    public Vector3 gps = new Vector3();
    public bool updatedGps;
    public object imuLock = new object();
    public Vector3[] imu = {new Vector3(), new Vector3(), new Vector3()};
    public bool updatedImu;
    public object envLock = new object();
    public float[] env = {0.0f, 0.0f, 0.0f};
    public bool updatedEnv;

    public bool connected;

    public bool isStopped;

    

    public ParserThread(TextUi textUI)
    {
        this.textUI = textUI;
        initPackets = new List<PacketBase>();
        gpsPackets = new List<GPSPacket>();
        imuPackets = new List<IMUPacket>();
        envPackets = new List<ENVPacket>();
        buffer = new RingBuffer();
        isStopped = false;
        updatedGps = false;
        updatedImu = false;
        updatedEnv = false;
        connected = false;

    }

    public void Run()
    {
        
        while (!textUI.connected)
        {
            try
            {
                serialPort = new SerialPort(textUI.currentPort, 9600);
                serialPort.Open();
                textUI.connected = true;
            } catch
            {

            }

        }


        while (!isStopped)
        {
            int read_result = serialPort.ReadByte();
            byte incoming_byte = (byte)read_result;
            if (read_result != -1)
            {
                buffer.Append(incoming_byte);

                if (incoming_byte == 0x16)
                {
                    initPackets.Add(new PacketBase(buffer, buffer.tip));
                }

                for (int i = initPackets.Count - 1; i >= 0; i--)
                {
                    if (initPackets[i].status == PacketStatus.OK || initPackets[i].status == PacketStatus.Rejected) initPackets.Remove(initPackets[i]);
                    else initPackets[i].Init(gpsPackets, imuPackets, envPackets);
                }
                for (int i = gpsPackets.Count - 1; i >= 0; i--)
                {
                    if (gpsPackets[i].status == PacketStatus.Rejected) { gpsPackets.Remove(gpsPackets[i]); }
                    else if (gpsPackets.Count > i && gpsPackets[i].status == PacketStatus.OK)
                    {
                        lock (gpsLock)
                        {
                            gps = gpsPackets[i].position;
                            updatedGps = true;
                        }
                        gpsPackets.Remove(gpsPackets[i]);

                    }
                    else if (gpsPackets.Count > i && gpsPackets[i].status == PacketStatus.NotRead) gpsPackets[i].Read();
                }
                for (int i = imuPackets.Count - 1; i >= 0; i--)
                {
                    if (imuPackets[i].status == PacketStatus.Rejected) { imuPackets.Remove(imuPackets[i]); }
                    else if(imuPackets.Count > i && imuPackets[i].status == PacketStatus.OK)
                    {
                        lock (imuLock)
                        {
                            imu[0] = imuPackets[i].mag;
                            imu[1] = imuPackets[i].accel;
                            imu[2] = imuPackets[i].gyro;
                            updatedImu = true;
                        }
                        imuPackets.Remove(imuPackets[i]);
                    }
                    else if(imuPackets.Count > i && imuPackets[i].status == PacketStatus.NotRead) imuPackets[i].Read();
                }
                for (int i = envPackets.Count - 1; i >= 0; i--)
                {
                    if (envPackets[i].status == PacketStatus.Rejected) { envPackets.Remove(envPackets[i]); }
                    else if(envPackets.Count > i && envPackets[i].status == PacketStatus.OK)
                    {
                        lock (envLock)
                        {
                            env[0] = envPackets[i].temp;
                            env[1] = envPackets[i].hum;
                            env[2] = envPackets[i].pressure;
                            updatedEnv = true;
                        }
                        Debug.Log("OK ENV");
                        envPackets.Remove(envPackets[i]);
                    }
                    else if(envPackets.Count > i && envPackets[i].status == PacketStatus.NotRead) envPackets[i].Read();
                }
            }
        }
        serialPort.Close();
    }

    public void Stop()
    {
        isStopped = true;
    }
}