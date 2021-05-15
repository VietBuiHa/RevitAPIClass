using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace WpfControlLibrary1.Windows.ViewModels
{
    public class DiggingHoleViewModel : BindableBase
    {
        private string topOffset = "1000";
        private string botOffset = "100";
        public bool Result;

        public string TopOffset
        {
            get { return topOffset; }
            set { SetProperty(ref topOffset, value); }
        }
        public string BotOffset
        {
            get { return botOffset; }
            set { SetProperty(ref botOffset, value); }
        }
        public ICommand OkCommand { get; }
        public ICommand CancelCommand { get; }

        public DiggingHoleViewModel()
        {
            OkCommand = new DelegateCommand(OkCommand_Handler);
            CancelCommand = new DelegateCommand(CancelCommand_Handler);
        }
        private void CancelCommand_Handler()
        {
            Result = false;
        }

        private void OkCommand_Handler()
        {
            Result = true;
        }
    }
}
