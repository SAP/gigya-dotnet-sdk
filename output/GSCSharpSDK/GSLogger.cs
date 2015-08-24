/*
 * Copyright (C) 2011 Gigya, Inc.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gigya.Socialize.SDK
{
    public class GSLogger
    {
        private StringBuilder sb = new StringBuilder();
        public void Write(Object data)
        {
            if (data == null) return;
            Write(null, data.ToString());
        }

        public void Write(Exception ex)
        {
            Write(ex.StackTrace);
        }

        public void Write(String key, Object data)
        {
            if (key != null)
                sb.Append(key + ": ");
            if (data != null)
                sb.Append(data.ToString() + "\r\n");
        }

        public void WriteFormat(String format, params Object[] args)
	    {
            
		    Write(String.Format(format, args));
	    }

        public override String ToString()
        {
            return sb.ToString();
        }
    }
}





