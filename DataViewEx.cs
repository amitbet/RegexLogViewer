using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace LogViewer
{
    class DataViewEx : DataView
    {
        public DataViewEx(DataTable p_dtTable) 
            :base(p_dtTable)
        {
        }

        public override DataRowView AddNew()
        {
            //TODO:check the regexp and conditionally add
            return base.AddNew();
        }

        //protected override void OnListChanged(System.ComponentModel.ListChangedEventArgs e)
        //{
        //    if (e.ListChangedType == System.ComponentModel.ListChangedType.ItemChanged)
        //    {
        //        //TODO:check the regexp
        //    }
        //    base.OnListChanged(e);
        //}
    }
}
