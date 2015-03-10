using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SourcePawn_IDE
{
    /// <summary>
    /// Interaction logic for AdmFlags.xaml
    /// </summary>
    public partial class AdmFlags : Window
    {
        public string Flags;
        public AdmFlags(string Flags)
        {
            InitializeComponent();
            this.Flags = Flags;
            ProcessCheckboxes();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            ProcessString();
            this.DialogResult = true;
        }

        void ProcessString()
        {
            StringBuilder builder = new StringBuilder();
            if (reservation.IsChecked.GetValueOrDefault(false))
            {
                builder.Append("ADMFLAG_RESERVATION|");
            }
            if (generic.IsChecked.GetValueOrDefault(false))
            {
                builder.Append("ADMFLAG_GENERIC|");
            }
            if (kick.IsChecked.GetValueOrDefault(false))
            {
                builder.Append("ADMFLAG_KICK|");
            }
            if (ban.IsChecked.GetValueOrDefault(false))
            {
                builder.Append("ADMFLAG_BAN|");
            }
            if (unban.IsChecked.GetValueOrDefault(false))
            {
                builder.Append("ADMFLAG_UNBAN|");
            }
            if (slay.IsChecked.GetValueOrDefault(false))
            {
                builder.Append("ADMFLAG_SLAY|");
            }
            if (changemap.IsChecked.GetValueOrDefault(false))
            {
                builder.Append("ADMFLAG_CHANGEMAP|");
            }
            if (convars.IsChecked.GetValueOrDefault(false))
            {
                builder.Append("ADMFLAG_CONVARS|");
            }
            if (config.IsChecked.GetValueOrDefault(false))
            {
                builder.Append("ADMFLAG_CONFIG|");
            }
            if (chat.IsChecked.GetValueOrDefault(false))
            {
                builder.Append("ADMFLAG_CHAT|");
            }
            if (vote.IsChecked.GetValueOrDefault(false))
            {
                builder.Append("ADMFLAG_VOTE|");
            }
            if (password.IsChecked.GetValueOrDefault(false))
            {
                builder.Append("ADMFLAG_PASSWORD|");
            }
            if (rcon.IsChecked.GetValueOrDefault(false))
            {
                builder.Append("ADMFLAG_RCON|");
            }
            if (cheats.IsChecked.GetValueOrDefault(false))
            {
                builder.Append("ADMFLAG_CHEATS|");
            }
            if (root.IsChecked.GetValueOrDefault(false))
            {
                builder.Append("ADMFLAG_ROOT|");
            }
            if (custom1.IsChecked.GetValueOrDefault(false))
            {
                builder.Append("ADMFLAG_CUSTOM1|");
            }
            if (custom2.IsChecked.GetValueOrDefault(false))
            {
                builder.Append("ADMFLAG_CUSTOM2|");
            }
            if (custom3.IsChecked.GetValueOrDefault(false))
            {
                builder.Append("ADMFLAG_CUSTOM3|");
            }
            if (custom4.IsChecked.GetValueOrDefault(false))
            {
                builder.Append("ADMFLAG_CUSTOM4|");
            }
            if (custom5.IsChecked.GetValueOrDefault(false))
            {
                builder.Append("ADMFLAG_CUSTOM5|");
            }
            if (custom6.IsChecked.GetValueOrDefault(false))
            {
                builder.Append("ADMFLAG_CUSTOM6|");
            }
            builder.Remove(builder.ToString().Length - 1, 1);
            Flags = builder.ToString();
        }

        void ProcessCheckboxes()
        {
            if (Flags.Length >= 0)
            {
                string[] split = Flags.Split('|');
                foreach (string str in split)
                {
                    switch (str)
                    {
                        case "ADMFLAG_RESERVATION":
                            reservation.IsChecked = true;
                            break;
                        case "ADMFLAG_GENERIC":
                            generic.IsChecked = true;
                            break;
                        case "ADMFLAG_KICK":
                            kick.IsChecked = true;
                            break;
                        case "ADMFLAG_BAN":
                            ban.IsChecked = true;
                            break;
                        case "ADMFLAG_UNBAN":
                            unban.IsChecked = true;
                            break;
                        case "ADMFLAG_SLAY":
                            slay.IsChecked = true;
                            break;
                        case "ADMFLAG_CHANGEMAP":
                            changemap.IsChecked = true;
                            break;
                        case "ADMFLAG_CONVARS":
                            convars.IsChecked = true;
                            break;
                        case "ADMFLAG_CONFIG":
                            config.IsChecked = true;
                            break;
                        case "ADMFLAG_CHAT":
                            chat.IsChecked = true;
                            break;
                        case "ADMFLAG_VOTE":
                            vote.IsChecked = true;
                            break;
                        case "ADMFLAG_PASSWORD":
                            password.IsChecked = true;
                            break;
                        case "ADMFLAG_RCON":
                            rcon.IsChecked = true;
                            break;
                        case "ADMFLAG_CHEATS":
                            cheats.IsChecked = true;
                            break;
                        case "ADMFLAG_ROOT":
                            root.IsChecked = true;
                            break;
                        case "ADMFLAG_CUSTOM1":
                            custom1.IsChecked = true;
                            break;
                        case "ADMFLAG_CUSTOM2":
                            custom2.IsChecked = true;
                            break;
                        case "ADMFLAG_CUSTOM3":
                            custom3.IsChecked = true;
                            break;
                        case "ADMFLAG_CUSTOM4":
                            custom4.IsChecked = true;
                            break;
                        case "ADMFLAG_CUSTOM5":
                            custom5.IsChecked = true;
                            break;
                        case "ADMFLAG_CUSTOM6":
                            custom6.IsChecked = true;
                            break;
                    }
                }
            }
        }
    }
}
