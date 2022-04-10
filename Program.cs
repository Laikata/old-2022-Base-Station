﻿using System.IO.Ports;

class RingBuffer {
    Byte[] buffer;
    public int tip{get; private set;}
    const int RING_BUFFER_SIZE = 256;

    public RingBuffer() {
        buffer = new Byte[RING_BUFFER_SIZE];
        tip = 0;
    }

    public Byte[] readBytes(int offset, int amount) {
        if(offset + amount > (RING_BUFFER_SIZE - 1)) {
            if(tip > offset && ((offset + amount) % RING_BUFFER_SIZE) < tip) {
                return new byte[0];
            }
        }
        if(offset + amount > tip) {
            return new byte[0];
        }

        Console.Write("Reading! Tip at: ");
        Console.WriteLine(tip);
        byte[] result = new byte[amount];
        for(int i = 0; i < amount; i++) {
            result[i] = buffer[(offset + i) % RING_BUFFER_SIZE];
        }
        return result;
    }

    public void Append(byte data) {
        buffer[tip] = data;
        tip = (tip + 1) % RING_BUFFER_SIZE;
    }
}
enum PacketTypes {INIT, GPS, IMU, ENV, INFO}
enum PacketStatus {NotRead, OK, Rejected};

class PacketBase {
    public int packetSize;
    public int offset;
    public PacketTypes packetType;
    public RingBuffer buffer;
    public PacketStatus status;

    public PacketBase(RingBuffer pbuffer, int poffset){
        packetSize = 0;
        packetType = PacketTypes.INIT;
        offset = poffset;
        buffer = pbuffer;
        status = PacketStatus.NotRead;
    }

    public void Init(List<GPSPacket> gpslist, List<IMUPacket> imulist, List<ENVPacket> envlist) {
        // Counter + Size + Protocol Header
        byte[] data = buffer.readBytes(offset, 3);
        if (data.Length != 0){
            offset += 2;

            byte packet_size = data[0];
            byte counter = data[1];
            byte packet_type = data[2];
            
            if(packet_size == 20 && packet_type == 0x01) {gpslist.Add(new GPSPacket(buffer, offset)); status = PacketStatus.OK;}
            else if(packet_size == 48 && packet_type == 0x02) {imulist.Add(new IMUPacket(buffer, offset)); status = PacketStatus.OK;}
            else if(packet_size == 20 && packet_type == 0x03) {envlist.Add(new ENVPacket(buffer, offset)); status = PacketStatus.OK;}
            else {Console.WriteLine("Rejected packet: Header/Size didn't match"); status = PacketStatus.Rejected;}
        }
    }
}

class GPSPacket : PacketBase {
    public float[] position;

    public GPSPacket(RingBuffer pbuffer, int poffset): base(pbuffer, poffset) {
        position = new float[3];
        packetType = PacketTypes.GPS;
    }
    public void Read(){
        byte[] data = buffer.readBytes(offset, 17);
        if(data.Length != 0) {
            Console.WriteLine(offset);
            position = new float[3];
            Console.WriteLine(BitConverter.ToString(data));
            position[0] = System.Buffers.Binary.BinaryPrimitives.ReadSingleBigEndian(data.AsSpan(1, 4));
            position[1] = System.Buffers.Binary.BinaryPrimitives.ReadSingleBigEndian(data.AsSpan(5, 4));
            position[2] = System.Buffers.Binary.BinaryPrimitives.ReadSingleBigEndian(data.AsSpan(9, 4));

            //TODO: Check CRC-32

            status = PacketStatus.OK;
        }
    }
}

class IMUPacket : PacketBase {
    public float[] mag;
    public float[] accel;
    public float[] gyro;

    public IMUPacket(RingBuffer pbuffer, int poffset): base(pbuffer, poffset) {
        mag = new float[3];
        accel = new float[3];
        gyro = new float[3];
        packetType = PacketTypes.IMU;
    }
}

class ENVPacket : PacketBase {
    public float temp;
    public float hum;
    public float pressure;

    public ENVPacket(RingBuffer pbuffer, int poffset): base(pbuffer, poffset) {
        temp = 0.0f;
        hum = 0.0f;
        pressure = 0.0f;
        packetType = PacketTypes.GPS;
    }
}

class Program {
    static void Main() {
        SerialPort serialPort = new SerialPort("COM0", 9600);
        List<PacketBase> initPackets = new List<PacketBase>();
        List<GPSPacket> gpsPackets = new List<GPSPacket>();
        List<IMUPacket> imuPackets = new List<IMUPacket>();
        List<ENVPacket> envPackets = new List<ENVPacket>();

        RingBuffer buffer = new RingBuffer();

        while(true) {
            //byte incoming_byte = (byte)serialPort.ReadByte();
            Byte[] incoming_byte = new Byte[1];
            Stream stdin = Console.OpenStandardInput();
            int read_bytes = stdin.Read(incoming_byte, 0, 1);
            buffer.Append(incoming_byte[0]);
            
            if(incoming_byte[0] == 0x16) {
                Console.WriteLine("New packet!");
                initPackets.Add(new PacketBase(buffer, buffer.tip));
            }
            for(int i = initPackets.Count - 1; i >= 0; i--) {
                if(initPackets[i].status == PacketStatus.OK || initPackets[i].status == PacketStatus.Rejected) initPackets.Remove(initPackets[i]);
                else initPackets[i].Init(gpsPackets, imuPackets, envPackets);
            }
            for(int i = gpsPackets.Count - 1; i >= 0; i--) {
                if(gpsPackets[i].status == PacketStatus.OK) Console.WriteLine(gpsPackets[i].position[2]);
                else gpsPackets[i].Read();
                if(gpsPackets[i].status == PacketStatus.OK || gpsPackets[i].status == PacketStatus.Rejected) gpsPackets.Remove(gpsPackets[i]);
            }
        }
    }
}
