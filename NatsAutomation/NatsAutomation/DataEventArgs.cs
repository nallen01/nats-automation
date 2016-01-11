using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NatsAutomation
{
    class DataEventArgs : EventArgs
    {
        private String DataType = null;
        private int Division = -1;

        public void setDataType(String type)
        {
            DataType = type;
        }
        public String getDataType()
        {
            return DataType;
        }

        public void setDivision(int div)
        {
            Division = div;
        }
        public int getDivision()
        {
            return Division;
        }
    }
}
