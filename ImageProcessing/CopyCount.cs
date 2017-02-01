using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageProcessing
{
    public class CopyCount
    {
        private int _count;
        private int _maxCount;
        public int Count
        {
            get
            {
                return this._count;
            }
            set
            {
                if (value > _maxCount)
                    this._count = this._maxCount;
                else if (value < 1)
                    this._count = 1;
                else
                    this._count = value;
            }
        }
        public CopyCount(int maxCount)
        {
            if (maxCount > 0 && maxCount <= 10)
                this._maxCount = maxCount;
            else if (maxCount > 10)
                this._maxCount = 10;
            else
                this._maxCount = 1;
            this._count = 1;
        }
    }
}
