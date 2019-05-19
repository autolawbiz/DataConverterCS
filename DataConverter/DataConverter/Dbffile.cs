using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jp.autolawbiz.DataConverter
{
    class Dbffile
    {
        private string fieldName;
        private string fieldType;
        private int fieldLength;
        private int fieldDecimalpartLength;
        private List<string> listValues;

        public Dbffile(List<string> listValues)
        {
            this.listValues = listValues;
        }

        public string FieldName { get => fieldName; set => fieldName = value; }
        public string FieldType { get => fieldType; set => fieldType = value; }
        public int FieldLength { get => fieldLength; set => fieldLength = value; }
        public int FieldDecimalpartLength { get => fieldDecimalpartLength; set => fieldDecimalpartLength = value; }
        public List<string> ListValues { get => listValues; set => listValues = value; }
    }
}
