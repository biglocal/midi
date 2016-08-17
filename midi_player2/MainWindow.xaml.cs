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
                mthd_c mthd = new mthd_c(MThd);
                for (int i = 0; i < mthd.track; i++)
                {
                    String purpose = "unknown";
                    foreach (meta_data_c meta in mthd.mtrk_list[i].meta_data_list)
                    {
                        purpose = meta.mtrk_purpose;
                        if(purpose == "Sequence or track name")
                        {
                            purpose = meta.track_name;
                        }
                    }
                    track_num.Items.Add("Track:" + (i+1).ToString()+"\t\t(Midi Count:"+ mthd.mtrk_list[i].midi_data_list.Count+ ")");
                }
                track_num.SelectedIndex = 2;
                show_track_notes(mthd.mtrk_list[2]);
                fileStream.Close();
            }
        }

        private void show_track_notes(mtrk_c mtrk)
        {
            if (mtrk.midi_data_list.Count == 0)
                concent_textbl.Text = "There is no note information.";
            else
            {
                foreach (midi_data_c midi in mtrk.midi_data_list)
                {
                    if(midi.midi_data != null)
                        concent_textbl.Text += midi.get_note(midi);
                }
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
            midi.Play(0x0060229F);
        }
    }
}
