using System;
using System.Runtime.InteropServices;
using BMDSwitcherAPI;

namespace NatsAutomation
{
    public class VisionService
    {
        private IBMDSwitcher Switcher;
        private IBMDSwitcherInputIterator InputIterator;

        public VisionService(IBMDSwitcher Switcher)
        {
            this.Switcher = Switcher;

            if(this.Switcher != null)
            {
                IntPtr input_iterator_ptr;
                this.Switcher.CreateIterator(typeof(IBMDSwitcherInputIterator).GUID, out input_iterator_ptr);
                if (input_iterator_ptr != null)
                {
                    this.InputIterator = (IBMDSwitcherInputIterator)Marshal.GetObjectForIUnknown(input_iterator_ptr);
                }
                else
                {
                    this.Switcher = null;
                }
            }
        }
    }
}
