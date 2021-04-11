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
    public class AutoJoinDialogViewModel : BindableBase
    {
        private string[] _listCategory;
        private string _category1;
        private string _category2;
        private bool _result;
        public bool Result
        {
            get { return _result; }
            set { SetProperty(ref _result, value); }
        }
        public string[] ListCategory
        {
            get { return _listCategory; }
            set { SetProperty(ref _listCategory, value); }
        }

        public string Category1
        {
            get { return _category1; }
            set { SetProperty(ref _category1, value); }
        }

        public string Category2
        {
            get { return _category2; }
            set { SetProperty(ref _category2, value); }
        }

        public ICommand OkCommand { get; }
        public ICommand CancelCommand { get; }

        public AutoJoinDialogViewModel(string[] list)
        {
            ListCategory = list;
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
