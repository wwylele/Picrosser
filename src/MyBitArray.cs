using System;
using System.Collections.Generic;
using System.Text;

namespace Picrosser {
    class MyBitArray {
        ulong[] bits;
        public MyBitArray(int size) {

            bits = new ulong[(size - 1) / 64 + 1];
        }
        public void Reset() {
            for(int i = 0; i < bits.Length; ++i) {
                bits[i] = 0;
            }
        }
        public void Set(int pos) {
            bits[pos / 64] |= 1UL << (pos % 64);
        }
        public void Not() {
            for(int i = 0; i < bits.Length; ++i) {
                bits[i] = ~bits[i];
            }
        }
        public void And(MyBitArray other) {
            for(int i = 0; i < bits.Length; ++i) {
                bits[i] &= other.bits[i];
            }
        }
        public void AndNot(MyBitArray other) {
            for(int i = 0; i < bits.Length; ++i) {
                bits[i] &= ~other.bits[i];
            }
        }
        public bool Test(int pos) {
            return (bits[pos / 64] & (1UL << (pos % 64))) != 0;
        }
        public bool AndIsZero(MyBitArray other) {
            for(int i = 0; i < bits.Length; ++i) {
                if((bits[i] & other.bits[i]) != 0) return false;
            }
            return true;
        }
        public bool NotAndIsZero(MyBitArray other) {
            for(int i = 0; i < bits.Length; ++i) {
                if(((~bits[i]) & other.bits[i]) != 0) return false;
            }
            return true;
        }
        public object Clone() {
            MyBitArray n = new MyBitArray(bits.Length * 64);
            for(int i = 0; i < bits.Length; ++i) {
                n.bits[i] = bits[i];
            }
            return n;
        }
    }
}
