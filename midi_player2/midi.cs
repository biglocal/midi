using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections;

namespace midi_player2
{

    class midi_c
    {
        public String name { get; set; }
        public int num_of_track { get; set; }
        public long len_midi { get; set; }

        [DllImport("winmm.dll")]
        private extern static int midiOutOpen(out int handle, int uDeviceID, int dwCallback, int dwInstance, int dwFlags);
        [DllImport("winmm.dll")]
        private extern static int midiOutClose(int lphMidiOut);
        [DllImport("winmm.dll")]
        public extern static int midiOutShortMsg(int lphMidiOut, int dwMsg);
        //[DllImport("winmm.dll")]
       // static extern Int32 mciSendString(String command, StringBuilder buffer, Int32 bufferSize, IntPtr hwndCallback);
        public int midiOut;
        public midi_c()
        {
            Debug.WriteLine("midi_c");
            int result = midiOutOpen(out midiOut, -1, 0, 0, 0);
            Debug.WriteLine("result:"+result);
            if (result != 0)
            {
                throw new Exception("無法打開MIDI設備");
            }
        }
        ~midi_c()
        {
            if (midiOut != 0)
            {
                midiOutClose(midiOut);
            }
        }
        /*
        public void playMidi(String fileName, String alias)
        {
            mciSendString("open " + fileName + " type sequencer alias " + alias, new StringBuilder(), 0, new IntPtr());
            mciSendString("play " + alias, new StringBuilder(), 0, new IntPtr());
        }

        public void stopMidi(String alias)
        {
            mciSendString("stop " + alias, null, 0, new IntPtr());
            mciSendString("close " + alias, null, 0, new IntPtr());
        }
        */
        public void Instrument(int instrument)
        {
            midiOutShortMsg(midiOut, instrument << 8 | 0xC0);
        }
        public void Play(int msg)
        {
            midiOutShortMsg(midiOut, msg);
        }
        public void Stop(int msg)
        {
            midiOutShortMsg(midiOut, msg);
        }
    }


    class mthd_c
    {
        public byte[] mthd_name = new byte[4];
        public byte[] len_of_data = new byte[4];
        public byte[] format = new byte[2];
        public int track { get; }
        public byte[] division = new byte[2];
        byte[] mtrk_pattern = new byte[] { 0x4D, 0x54, 0x72, 0x6B };
        int[] mtrk_offset;
        public List<mtrk_c> mtrk_list = new List<mtrk_c>();

        public mthd_c(byte[] data)
        {
            Debug.WriteLine("mthd_c");
            byte[] tmp = new byte[2];
            Array.Copy(data, 0, mthd_name, 0, 4);
            Array.Copy(data, 4, len_of_data, 0, 4);
            Array.Copy(data, 8, format, 0, 2);
            Array.Copy(data, 10, tmp, 0, 2);
            Array.Copy(data, 12, division, 0, 2);

            if (BitConverter.IsLittleEndian)
                Array.Reverse(tmp);
            track = BitConverter.ToUInt16(tmp, 0);
            Debug.WriteLine(BitConverter.ToString(mthd_name) + " " + BitConverter.ToString(len_of_data) + " " + BitConverter.ToString(format) + " " + track);
            mtrk_offset = new int[track];
            int index = 0;
            for (int i = 14; i < data.Length; i++)
            {
                if (data[i].CompareTo(mtrk_pattern[0]) != 0)
                    continue;
                if (data[i + 1].CompareTo(mtrk_pattern[1]) != 0)
                    continue;
                if (data[i + 2].CompareTo(mtrk_pattern[2]) != 0)
                    continue;
                if (data[i + 3].CompareTo(mtrk_pattern[3]) != 0)
                    continue;
                mtrk_offset[index] = i;
                index++;
            }

            for (int i = 0; i < track; i++)
            {
                int len = 0;
                if (i != (track - 1))
                {
                    len = mtrk_offset[i + 1] - mtrk_offset[i];
                }
                else
                {
                    len = data.Length - mtrk_offset[i];
                }
                mtrk_c mtrk = new mtrk_c(data, mtrk_offset[i], len);
                mtrk_list.Add(mtrk);
                Debug.Write("[Track " + i + "]");
            }
        }
    }

    class mtrk_c
    {
        public byte[] mtrk_name = new byte[4];
        public int len_of_data { get; }
        byte[] meta_data_pattern = new byte[] { 0x00, 0xFF };
        public List<meta_data_c> meta_data_list = new List<meta_data_c>();
        public List<midi_data_c> midi_data_list = new List<midi_data_c>();
        public bool is_note_on = false;
        public int midi_num = 0;
        public mtrk_c(byte[] data, int start, int len_of_track)
        {
            byte[] tmp = new byte[4];
            Array.Copy(data, start, mtrk_name, 0, 4);
            Array.Copy(data, start + 4, tmp, 0, 4);

            if (BitConverter.IsLittleEndian)
                Array.Reverse(tmp);
            len_of_data = BitConverter.ToUInt16(tmp, 0);
            
            int end_of_track_index = start + len_of_track;
            //Debug.WriteLine("start:" + String.Format("{0:X}", start) + ", end_of_track_index:" + String.Format("{0:X}", end_of_track_index));
            double time = 0;
            for (int i = start + 8;i < end_of_track_index; i++)
            {
                if (data[i] > 0x80 && data[i + 1] > 0x80)                   //v_time len is 3 byte
                {
                    time += cal_v_time(data[i], data[i + 1], data[i+2]);
                    if (data[i + 3].CompareTo(meta_data_pattern[1]) == 0)   //v_time+meta_event
                    {
                        i = i + create_meta_data(data, i + 3) + 3;
                    }
                    else if (data[i + 3] >= 0x80)                           //Midi_event
                    {
                        i = i + create_midi_data(data, time, i + 3, is_note_on) + 3;
                    }
                    else if (is_note_on)                                    
                    {
                        i = i + create_midi_data(data, time, i + 3, is_note_on) + 3;
                    }
                }
                else if (data[i] > 0x80)                                    //v_time len is 2 byte
                {
                    time += cal_v_time(data[i], data[i+1]);
                    if (data[i + 2].CompareTo(meta_data_pattern[1]) == 0)
                    {
                        i = i + create_meta_data(data, i + 2) + 2;
                    }
                    else if (data[i + 2] >= 0x80)                           //Midi_event
                    {
                        i = i + create_midi_data(data, time, i + 2, is_note_on) + 2;
                    }
                    else if (is_note_on)
                    {
                        i = i + create_midi_data(data, time, i + 2, is_note_on) + 2;
                    }
                }
                else                                                       //v_time len is 1 byte
                {
                    time += cal_v_time(data[i]);
                    if (data[i + 1].CompareTo(meta_data_pattern[1]) == 0)  //v_time+meta_event
                    {
                        i = i + create_meta_data(data, i + 1) + 1;
                    }
                    else if (data[i + 1] >= 0x80)                           //Midi_event
                    {
                        i = i + create_midi_data(data, time, i + 1, is_note_on) + 1;
                    }
                    else if (is_note_on)
                    {
                        i = i + create_midi_data(data, time, i + 1, is_note_on) + 1;
                    }
                }
            }
        }

        private double cal_v_time(byte data)
        {
            double v_time = (byte)(data & 0x7F);
            return v_time;
        }

        private double cal_v_time(byte data, byte data1)
        {
            double v_time = 0;
            BitArray hsb_byte = new BitArray(new byte[] { data });
            BitArray lsb_byte = new BitArray(new byte[] { data1 });
            
            for (int i = 0; i < 7; i++)
            {
                if (lsb_byte[i] == true)
                {
                    v_time += Math.Pow(2, i);
                }
            }
            for (int i = 0; i < 7; i++)
            {
                if(hsb_byte[i] == true)
                {
                    v_time += Math.Pow(2, i+7);
                }
            }
            return v_time;
        }

        private double cal_v_time(byte data, byte data1, byte data2)
        {
            double v_time = 0;
            BitArray hsb_byte = new BitArray(new byte[] { data });
            BitArray msb_byte = new BitArray(new byte[] { data1 });
            BitArray lsb_byte = new BitArray(new byte[] { data2 });

            for (int i = 0; i < 7; i++)
            {
                if (lsb_byte[i] == true)
                {
                    v_time += Math.Pow(2, i);
                }
            }
            for (int i = 0; i < 7; i++)
            {
                if (msb_byte[i] == true)
                {
                    v_time += Math.Pow(2, i + 7);
                }
            }
            for (int i = 0; i < 7; i++)
            {
                if (hsb_byte[i] == true)
                {
                    v_time += Math.Pow(2, i + 14);
                }
            }
            return v_time;
        }

        private int create_meta_data(byte[] data, int index)
        {
            int next_data = 0;
            meta_data_c meta = new meta_data_c(data, index, out next_data);
            meta_data_list.Add(meta);
            return next_data;
        }

        private int create_midi_data(byte[] data, double time, int index, bool is_note_on)
        {
            int next_data = 0;
            midi_num++;
            midi_data_c midi = new midi_data_c(data, time, index, midi_num, out next_data);
            this.is_note_on = midi.countinues;
            midi_data_list.Add(midi);
            return next_data;
        }
    }

    class midi_data_c
    {
        public int num { get; }
        public double v_time { get; }
        public byte status;
        public byte midi_data { get; }
        public byte vel_data { get; }
        public bool countinues = false;
        public string operation { get; }
        public midi_data_c(byte[] data, double time, int index, int no, out int offset)
        {
            num = no;   
            status = data[index];
            v_time = time;
            //Debug.WriteLine("status: "+String.Format("{0:X}", status)+", index: "+ String.Format("{0:X}", index));
            //2 byte, note on, note off, Polyphonic aftertouch, Control mode change
            if (status >= 0x80 && status <= 0xBF)     
            {
                midi_data = data[index + 1];
                vel_data = data[index + 2];
                offset = 2;
                operation = "note on";
                if (status <= 0x80 && status <= 0x8f)
                {
                    operation = "note off";
                }
                countinues = true;
            }
            //1 byte,Program change, Channel aftertouch
            else if (status >= 0xC0 && status <= 0xDF) 
            {
                midi_data = data[index + 1];
                offset = 1;
                operation = "Program change or Channel aftertouch";
                
            }
            //2 byte, Pitch wheel range
            else if (status >= 0xE0 && status <= 0xEF) 
            {
                midi_data = data[index + 1];
                vel_data = data[index + 2];
                offset = 2;
            }
            //System Exclusive
            else if (status >= 0xF0)
            {
                midi_data = 0;
                vel_data = 0;
                offset = 1;
            }
            else // no note on/off status, just use vel to control
            {
                midi_data = data[index];
                vel_data = data[index + 1];
                
                status = 0x90;
                operation = "note on";
                
                if (vel_data == 0x00)
                {
                    status = 0x80;
                    operation = "note off";
                }
                if (v_time == 0)
                {
                    operation = "controller";
                }
                offset = 1;
                countinues = true;
            }

            //if(midi_data != null)
            //    Debug.WriteLine("Operation: "+ String.Format("{0:X}", status) + ", MiDi_data: "+BitConverter.ToString(midi_data));
        }

        public string get_note(midi_data_c data)
        {
            if (data.status >= 0x80 && data.status <= 0x9F)
            {
                string str = String.Format("{0:D} {1:D} ", Convert.ToInt32(data.midi_data), Convert.ToInt32(data.vel_data));
                Debug.WriteLine(str);
                return str;
            }
            else
                return null;
        }
    }

    class meta_data_c
    {
        public byte meta_type;
        public byte[] meta_data;
        public int meta_len;
        public string mtrk_purpose { get; }
        public string track_name { get; }

        public meta_data_c(byte[] data, int index, out int offset)
        {
            meta_type = data[index + 1];
            
            byte[] tmp = new byte[4];
            Array.Copy(data, index + 2, tmp, 0, 1);
            meta_len = data[index + 2];

            meta_data = new byte[data[index + 2]];
            Array.Copy(data, index + 3, meta_data, 0, data[index + 2]);

            offset = meta_len + 2;
            Debug.WriteLine(String.Format("{0:X}", meta_type) + ", meta_len:" + meta_len + ", meta_data:" + BitConverter.ToString(meta_data) + ", " + String.Format("{0:X}", index)+ ", offset:" + String.Format("{0:X}", offset));

            switch(meta_type)
            {
                case (0x00):
                    mtrk_purpose = "Sequence number";
                    break;
                case (0x01):
                    mtrk_purpose = "Text event";
                    break;
                case (0x02):
                    mtrk_purpose = "Copyright notice";
                    break;
                case (0x03):
                    mtrk_purpose = "Sequence or track name";
                    track_name = System.Text.Encoding.ASCII.GetString(meta_data);
                    break;
                case (0x04):
                    mtrk_purpose = "Instrument name";
                    break;
                case (0x05):
                    mtrk_purpose = "Lyric text";
                    break;
                case (0x06):
                    mtrk_purpose = "Marker text";
                    break;
                case (0x07):
                    mtrk_purpose = "Cue point";
                    break;
                case (0x20):
                    mtrk_purpose = "MIDI channel prefix assignment";
                    break;
                case (0x2F):
                    mtrk_purpose = "End of track";
                    break;
                case (0x51):
                    mtrk_purpose = "Tempo setting";
                    break;
                case (0x54):
                    mtrk_purpose = "SMPTE offset";
                    break;
                case (0x58):
                    mtrk_purpose = "Time signature";
                    break;
                case (0x59):
                    mtrk_purpose = "Key signature";
                    break;
                case (0x7F):
                    mtrk_purpose = "Sequencer specific event";
                    break;
                default:
                    break;
            }
        }
    }
}
