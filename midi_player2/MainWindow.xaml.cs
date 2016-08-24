using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using System.IO;


namespace midi_player2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    
    public partial class MainWindow : Window
    {
        /****/
        const int MIM_DATA = 0x3C3;
        const int MIM_ERROR = 0x3C5;
        const int MIM_LONGDATA = 0x3C4;
        /****/
        private midi_c midi = new midi_c();
        private mthd_c mthd;
        static int play_index = 0;
        static int selected_track_num=0;
        public MainWindow()
        {
            InitializeComponent();
            num_of_midi_in.Content = midi.num_midi_in.ToString();
            midi.messageHandler = new MidiInProc(OnMessage);
            midi.initial_midi_in();
        }
        

        private void open_midi_file(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            // Set filter for file extension and default file extension 
            dlg.DefaultExt = ".mid";
            dlg.Filter = "MIDI Files (*.mid) | *mid";


            // Display OpenFileDialog by calling ShowDialog method 
            Nullable<bool> result = dlg.ShowDialog();
            if (result == true)
            {
                Debug.WriteLine("File path:" + dlg.FileName);
                midi_file.Content = dlg.FileName;

                midi.name = dlg.FileName;

                FileStream fileStream = new FileStream(midi.name, FileMode.Open);
                byte[] MThd = new byte[fileStream.Length];
                midi.len_midi = fileStream.Length;
                int n = fileStream.Read(MThd, 0, (int)fileStream.Length);
                mthd = new mthd_c(MThd);
                for (int i = 0; i < mthd.track; i++)
                {
                    track_num.Items.Add("Track:" + (i+1).ToString()+"\t\t(Midi Count:"+ mthd.mtrk_list[i].midi_data_list.Count+ ")");
                }
                track_num.SelectedIndex = 1;
                selected_track_num = track_num.SelectedIndex;
                show_track_notes(mthd.mtrk_list[selected_track_num]);
                fileStream.Close();
            }
        }

        private void show_track_notes(mtrk_c mtrk)
        {
            
            if (mtrk.midi_data_list.Count != 0)
            { 
                mididata_Grid.ItemsSource = mtrk.midi_data_list;
            }
        }

        private void midi_stop(object sender, RoutedEventArgs e)
        {
            //midi.stopMidi("TeaTimerAudio");
        }

        private void midi_signle_note(object sender, RoutedEventArgs e)
        {
            //midi.Play(volumn << 16 | note << 8 | 0x90);
        }

        private void change_track(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;

            selected_track_num = comboBox.SelectedIndex;
            show_track_notes(mthd.mtrk_list[selected_track_num]);
            play_index = 0;
        }

        private void midi_signle_note(byte vel, byte note)
        {
            Debug.WriteLine("vel:" + vel + " note:"+ note);
            midi.Play(vel << 16 | note << 8 | 0x90);
        }

        private void play_tone_on_select(object sender, SelectedCellsChangedEventArgs e)
        {
            return;
            DataGrid dg = sender as DataGrid;
            midi_data_c midi = (midi_data_c)dg.SelectedItems[0];

            midi_signle_note(midi.vel_data, midi.midi_data);
        }

        private int find_note_index()
        {
            int result = -1;
            int count = mthd.mtrk_list[selected_track_num].midi_data_list.Count;
            for(int i = play_index;i<count;i++)
            {
                if(mthd.mtrk_list[selected_track_num].midi_data_list[i].operation == "note on")
                {
                    return i;
                }
            }
            return result;
        }

        private void OnMessage(int handle, int msg, int instance, int param1, int param2)
        {
            if (msg == MIM_DATA || msg == MIM_ERROR || msg == MIM_LONGDATA)
            {
                byte status = (byte)(param1 & 0x000090);
                if(status == 0x80)          //note off
                {

                }
                else if(status == 0x90)     //note on
                {
                    int tone = find_note_index();

                    if (tone == -1)
                    {
                        play_index = 0;
                        tone = find_note_index();
                    }
                    midi_signle_note(mthd.mtrk_list[selected_track_num].midi_data_list[tone].vel_data, mthd.mtrk_list[selected_track_num].midi_data_list[tone].midi_data);
                    play_index = tone;


                    Debug.WriteLine("Play_index:"+ play_index);
                    DataGridRow row = (DataGridRow)mididata_Grid.ItemContainerGenerator.ContainerFromIndex(play_index);
                    Action methodDelegate = delegate ()
                    {
                        object item = mididata_Grid.Items[play_index - 1];
                        mididata_Grid.SelectedItem = item;
                        mididata_Grid.ScrollIntoView(item);
                    };
                    this.Dispatcher.BeginInvoke(methodDelegate);
                    play_index ++;
                }
            }
        }

        private void midi_reset(object sender, RoutedEventArgs e)
        {
            play_index = 0;
            DataGridRow row = (DataGridRow)mididata_Grid.ItemContainerGenerator.ContainerFromIndex(play_index);
            Action methodDelegate = delegate ()
            {
                object item = mididata_Grid.Items[play_index];
                mididata_Grid.SelectedItem = item;
                mididata_Grid.ScrollIntoView(item);
            };
            this.Dispatcher.BeginInvoke(methodDelegate);
        }
    }
}
