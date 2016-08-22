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
    public partial class MainWindow : Window
    {
        private midi_c midi = new midi_c();
        private mthd_c mthd;
        static int play_index = 0;

        public MainWindow()
        {
            InitializeComponent();
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
                show_track_notes(mthd.mtrk_list[1]);
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

        private void midi_play(object sender, RoutedEventArgs e)
        {
            //midi.playMidi(midi.name, "TeaTimerAudio");
        }

        private void midi_signle_note(object sender, RoutedEventArgs e)
        {
            //midi.Play(volumn << 16 | note << 8 | 0x90);
        }

        private void chenge_track(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            int track_num = 0 ;

            track_num = comboBox.SelectedIndex;
            show_track_notes(mthd.mtrk_list[track_num]);
        }

        private void midi_signle_note(byte vel, byte note)
        {
            midi.Play(vel << 16 | note << 8 | 0x90);
        }

        private void play_tone_on_select(object sender, SelectedCellsChangedEventArgs e)
        {
            DataGrid dg = sender as DataGrid;
            midi_data_c midi = (midi_data_c)dg.SelectedItems[0];
            Debug.WriteLine(midi.operation);
            midi_signle_note(midi.vel_data, midi.midi_data);
        }
    }
}
