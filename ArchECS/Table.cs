using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ArchECS
{
   

    public class Table : IDisposable
    {
        internal class TableLookupComparer : IEqualityComparer<TableLookup>
        {
            public bool Equals(TableLookup x, TableLookup y)
            {
                return x.CompareTo(y) == 0;
            }

            public int GetHashCode([DisallowNull] TableLookup obj)
            {
                return HashCode.Combine<int, int, int, int, int, int, int, int>(obj.bit1.Data, obj.bit2.Data, obj.bit3.Data, obj.bit4.Data, obj.bit5.Data, obj.bit6.Data, obj.bit7.Data, obj.bit8.Data);
            }
        }
        [StructLayout(LayoutKind.Explicit, Size = 32)]
        internal struct TableLookup : IComparable<TableLookup>
        {
            [FieldOffset(0)]
            int bit1Data;
            [FieldOffset(0)]
            internal BitVector32 bit1;
            [FieldOffset(4)]
            internal BitVector32 bit2;
            [FieldOffset(8)]
            internal BitVector32 bit3;
            [FieldOffset(12)]
            internal BitVector32 bit4;
            [FieldOffset(16)]
            internal BitVector32 bit5;
            [FieldOffset(20)]
            internal BitVector32 bit6;
            [FieldOffset(24)]
            internal BitVector32 bit7;
            [FieldOffset(28)]
            internal BitVector32 bit8;
            [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
            [SkipLocalsInit]
            public TableLookup(Span<byte> componentIds)
            {
                bit1Data = 0;
                bit1 = default(BitVector32);
                bit2 = default(BitVector32);
                bit3 = default(BitVector32);
                bit4 = default(BitVector32);
                bit5 = default(BitVector32);
                bit6 = default(BitVector32);
                bit7 = default(BitVector32);
                bit8 = default(BitVector32);

                
                for(int i = 0; i < componentIds.Length; i++)
                {
                    switch (componentIds[i])
                    {
                        case 000: bit1[1 << 000] = true; break;
                        case 001: bit1[1 << 001] = true; break;
                        case 002: bit1[1 << 002] = true; break;
                        case 003: bit1[1 << 003] = true; break;
                        case 004: bit1[1 << 004] = true; break;
                        case 005: bit1[1 << 005] = true; break;
                        case 006: bit1[1 << 006] = true; break;
                        case 007: bit1[1 << 007] = true; break;
                        case 008: bit1[1 << 008] = true; break;
                        case 009: bit1[1 << 009] = true; break;
                        case 010: bit1[1 << 010] = true; break;
                        case 011: bit1[1 << 011] = true; break;
                        case 012: bit1[1 << 012] = true; break;
                        case 013: bit1[1 << 013] = true; break;
                        case 014: bit1[1 << 014] = true; break;
                        case 015: bit1[1 << 015] = true; break;
                        case 016: bit1[1 << 016] = true; break;
                        case 017: bit1[1 << 017] = true; break;
                        case 018: bit1[1 << 018] = true; break;
                        case 019: bit1[1 << 019] = true; break;
                        case 020: bit1[1 << 020] = true; break;
                        case 021: bit1[1 << 021] = true; break;
                        case 022: bit1[1 << 022] = true; break;
                        case 023: bit1[1 << 023] = true; break;
                        case 024: bit1[1 << 024] = true; break;
                        case 025: bit1[1 << 025] = true; break;
                        case 026: bit1[1 << 026] = true; break;
                        case 027: bit1[1 << 027] = true; break;
                        case 028: bit1[1 << 028] = true; break;
                        case 029: bit1[1 << 029] = true; break;
                        case 030: bit1[1 << 030] = true; break;
                        case 031: bit1[1 << 031] = true; break;
                        case 032: bit2[033] = true; break;
                        case 033: bit2[034] = true; break;
                        case 034: bit2[035] = true; break;
                        case 035: bit2[036] = true; break;
                        case 036: bit2[037] = true; break;
                        case 037: bit2[038] = true; break;
                        case 038: bit2[039] = true; break;
                        case 039: bit2[040] = true; break;
                        case 040: bit2[041] = true; break;
                        case 041: bit2[042] = true; break;
                        case 042: bit2[043] = true; break;
                        case 043: bit2[044] = true; break;
                        case 044: bit2[045] = true; break;
                        case 045: bit2[046] = true; break;
                        case 046: bit2[047] = true; break;
                        case 047: bit2[048] = true; break;
                        case 048: bit2[049] = true; break;
                        case 049: bit2[050] = true; break;
                        case 050: bit2[051] = true; break;
                        case 051: bit2[052] = true; break;
                        case 052: bit2[053] = true; break;
                        case 053: bit2[054] = true; break;
                        case 054: bit2[055] = true; break;
                        case 055: bit2[056] = true; break;
                        case 056: bit2[057] = true; break;
                        case 057: bit2[058] = true; break;
                        case 058: bit2[059] = true; break;
                        case 059: bit2[060] = true; break;
                        case 060: bit2[061] = true; break;
                        case 061: bit2[062] = true; break;
                        case 062: bit2[063] = true; break;
                        case 063: bit2[064] = true; break;
                        case 064: bit3[065] = true; break;
                        case 065: bit3[066] = true; break;
                        case 066: bit3[067] = true; break;
                        case 067: bit3[068] = true; break;
                        case 068: bit3[069] = true; break;
                        case 069: bit3[070] = true; break;
                        case 070: bit3[071] = true; break;
                        case 071: bit3[072] = true; break;
                        case 072: bit3[073] = true; break;
                        case 073: bit3[074] = true; break;
                        case 074: bit3[075] = true; break;
                        case 075: bit3[076] = true; break;
                        case 076: bit3[077] = true; break;
                        case 077: bit3[078] = true; break;
                        case 078: bit3[079] = true; break;
                        case 079: bit3[080] = true; break;
                        case 080: bit3[081] = true; break;
                        case 081: bit3[082] = true; break;
                        case 082: bit3[083] = true; break;
                        case 083: bit3[084] = true; break;
                        case 084: bit3[085] = true; break;
                        case 085: bit3[086] = true; break;
                        case 086: bit3[087] = true; break;
                        case 087: bit3[088] = true; break;
                        case 088: bit3[089] = true; break;
                        case 089: bit3[090] = true; break;
                        case 090: bit3[091] = true; break;
                        case 091: bit3[092] = true; break;
                        case 092: bit3[093] = true; break;
                        case 093: bit3[094] = true; break;
                        case 094: bit3[095] = true; break;
                        case 095: bit3[096] = true; break;
                        case 096: bit4[097] = true; break;
                        case 097: bit4[098] = true; break;
                        case 098: bit4[099] = true; break;
                        case 099: bit4[100] = true; break;
                        case 100: bit4[101] = true; break;
                        case 101: bit4[102] = true; break;
                        case 102: bit4[103] = true; break;
                        case 103: bit4[104] = true; break;
                        case 104: bit4[105] = true; break;
                        case 105: bit4[106] = true; break;
                        case 106: bit4[107] = true; break;
                        case 107: bit4[108] = true; break;
                        case 108: bit4[109] = true; break;
                        case 109: bit4[110] = true; break;
                        case 110: bit4[111] = true; break;
                        case 111: bit4[112] = true; break;
                        case 112: bit4[113] = true; break;
                        case 113: bit4[114] = true; break;
                        case 114: bit4[115] = true; break;
                        case 115: bit4[116] = true; break;
                        case 116: bit4[117] = true; break;
                        case 117: bit4[118] = true; break;
                        case 118: bit4[119] = true; break;
                        case 119: bit4[120] = true; break;
                        case 120: bit4[121] = true; break;
                        case 121: bit4[122] = true; break;
                        case 122: bit4[123] = true; break;
                        case 123: bit4[124] = true; break;
                        case 124: bit4[125] = true; break;
                        case 125: bit4[126] = true; break;
                        case 126: bit4[127] = true; break;
                        case 127: bit4[128] = true; break;
                        case 128: bit5[129] = true; break;
                        case 129: bit5[130] = true; break;
                        case 130: bit5[131] = true; break;
                        case 131: bit5[132] = true; break;
                        case 132: bit5[133] = true; break;
                        case 133: bit5[134] = true; break;
                        case 134: bit5[135] = true; break;
                        case 135: bit5[136] = true; break;
                        case 136: bit5[137] = true; break;
                        case 137: bit5[138] = true; break;
                        case 138: bit5[139] = true; break;
                        case 139: bit5[140] = true; break;
                        case 140: bit5[141] = true; break;
                        case 141: bit5[142] = true; break;
                        case 142: bit5[143] = true; break;
                        case 143: bit5[144] = true; break;
                        case 144: bit5[145] = true; break;
                        case 145: bit5[146] = true; break;
                        case 146: bit5[147] = true; break;
                        case 147: bit5[148] = true; break;
                        case 148: bit5[149] = true; break;
                        case 149: bit5[150] = true; break;
                        case 150: bit5[151] = true; break;
                        case 151: bit5[152] = true; break;
                        case 152: bit5[153] = true; break;
                        case 153: bit5[154] = true; break;
                        case 154: bit5[155] = true; break;
                        case 155: bit5[156] = true; break;
                        case 156: bit5[157] = true; break;
                        case 157: bit5[158] = true; break;
                        case 158: bit5[159] = true; break;
                        case 159: bit5[160] = true; break;
                        case 160: bit6[161] = true; break;
                        case 161: bit6[162] = true; break;
                        case 162: bit6[163] = true; break;
                        case 163: bit6[164] = true; break;
                        case 164: bit6[165] = true; break;
                        case 165: bit6[166] = true; break;
                        case 166: bit6[167] = true; break;
                        case 167: bit6[168] = true; break;
                        case 168: bit6[169] = true; break;
                        case 169: bit6[170] = true; break;
                        case 170: bit6[171] = true; break;
                        case 171: bit6[172] = true; break;
                        case 172: bit6[173] = true; break;
                        case 173: bit6[174] = true; break;
                        case 174: bit6[175] = true; break;
                        case 175: bit6[176] = true; break;
                        case 176: bit6[177] = true; break;
                        case 177: bit6[178] = true; break;
                        case 178: bit6[179] = true; break;
                        case 179: bit6[180] = true; break;
                        case 180: bit6[181] = true; break;
                        case 181: bit6[182] = true; break;
                        case 182: bit6[183] = true; break;
                        case 183: bit6[184] = true; break;
                        case 184: bit6[185] = true; break;
                        case 185: bit6[186] = true; break;
                        case 186: bit6[187] = true; break;
                        case 187: bit6[188] = true; break;
                        case 188: bit6[189] = true; break;
                        case 189: bit6[190] = true; break;
                        case 190: bit6[191] = true; break;
                        case 191: bit6[192] = true; break;
                        case 192: bit7[193] = true; break;
                        case 193: bit7[194] = true; break;
                        case 194: bit7[195] = true; break;
                        case 195: bit7[196] = true; break;
                        case 196: bit7[197] = true; break;
                        case 197: bit7[198] = true; break;
                        case 198: bit7[199] = true; break;
                        case 199: bit7[200] = true; break;
                        case 200: bit7[201] = true; break;
                        case 201: bit7[202] = true; break;
                        case 202: bit7[203] = true; break;
                        case 203: bit7[204] = true; break;
                        case 204: bit7[205] = true; break;
                        case 205: bit7[206] = true; break;
                        case 206: bit7[207] = true; break;
                        case 207: bit7[208] = true; break;
                        case 208: bit7[209] = true; break;
                        case 209: bit7[210] = true; break;
                        case 210: bit7[211] = true; break;
                        case 211: bit7[212] = true; break;
                        case 212: bit7[213] = true; break;
                        case 213: bit7[214] = true; break;
                        case 214: bit7[215] = true; break;
                        case 215: bit7[216] = true; break;
                        case 216: bit7[217] = true; break;
                        case 217: bit7[218] = true; break;
                        case 218: bit7[219] = true; break;
                        case 219: bit7[220] = true; break;
                        case 220: bit7[221] = true; break;
                        case 221: bit7[222] = true; break;
                        case 222: bit7[223] = true; break;
                        case 223: bit7[224] = true; break;
                        case 224: bit8[225] = true; break;
                        case 225: bit8[226] = true; break;
                        case 226: bit8[227] = true; break;
                        case 227: bit8[228] = true; break;
                        case 228: bit8[229] = true; break;
                        case 229: bit8[230] = true; break;
                        case 230: bit8[231] = true; break;
                        case 231: bit8[232] = true; break;
                        case 232: bit8[233] = true; break;
                        case 233: bit8[234] = true; break;
                        case 234: bit8[235] = true; break;
                        case 235: bit8[236] = true; break;
                        case 236: bit8[237] = true; break;
                        case 237: bit8[238] = true; break;
                        case 238: bit8[239] = true; break;
                        case 239: bit8[240] = true; break;
                        case 240: bit8[241] = true; break;
                        case 241: bit8[242] = true; break;
                        case 242: bit8[243] = true; break;
                        case 243: bit8[244] = true; break;
                        case 244: bit8[245] = true; break;
                        case 245: bit8[246] = true; break;
                        case 246: bit8[247] = true; break;
                        case 247: bit8[248] = true; break;
                        case 248: bit8[249] = true; break;
                        case 249: bit8[250] = true; break;
                        case 250: bit8[251] = true; break;
                        case 251: bit8[252] = true; break;
                        case 252: bit8[253] = true; break;
                        case 253: bit8[254] = true; break;
                        case 254: bit8[255] = true; break;
                        case 255: bit8[256] = true; break;

                    }
                }
            }

            public unsafe int CompareTo(TableLookup other)
            {
                fixed(int* bits = &bit1Data)
                {
                    int* otherBits = &other.bit1Data;
                    var intSpan = new Span<int>(&bits, 8);
                    var otherSpan = new Span<int>(&bits, 8);
                    return intSpan.SequenceCompareTo(otherSpan);
                }

            }
        }

        internal void Clear()
        {
            Array.Clear(_indicesToUIDs, 0, _count);
            _count = 0;

            foreach (var buf in _buffers)
            {
                buf.Clear();
            }
            _emptySlots.Clear();
        }

        private World _world;
        private int[] _components;
        private Dictionary<int, int> _componentLookup;
        public int Count => _count;
        private int _count = 0;
        private long[] _indicesToUIDs = ArrayPool<long>.Shared.Rent(1024);
        internal SortedSet<int> _emptySlots;
        private ComponentBuffer[] _buffers; //index using _components
        internal int[] ComponentIds => _components;
        internal Type[] ComponentTypes;
        public Table(World world, int[] components)
        {
            _emptySlots = new SortedSet<int>();
            _world = world;
            int bufferCount;

            ComponentTypes = new Type[components.Length];
            for(int i = 0; i < components.Length; i++)
            {
                ComponentTypes[i] = world._components[components[i]].TargetType;
            }

            _componentLookup = new Dictionary<int, int>();
            (_components, bufferCount) = Component.RealComponentCount(components, _world);
            _buffers = new ComponentBuffer[Math.Max(0, bufferCount)];

            for (int i = 0; i < bufferCount; i++)
            {
                _componentLookup.Add(components[i], i);
                _buffers[i] = _world.GetBufferForComponent(_components[i], this);
            }
        }

        protected void Dispose(bool finalizer)
        {
            if (!finalizer)
                GC.SuppressFinalize(this);

            ArrayPool<long>.Shared.Return(_indicesToUIDs, true);
            _indicesToUIDs = null;

            foreach (var buff in _buffers)
            {
                buff.Dispose();
            }
            Array.Clear(_buffers, 0, _buffers.Length);
        }

        ~Table()
        {
            Dispose(true);
        }

        public unsafe struct EntityEnumerator
        {
            public MemoryHandle dataHandle;
            public long* data;
            public uint length;
            public uint current;
            SortedSet<int>.Enumerator emptySlotEnumerator;
            bool hasEmptySlots;
            public EntityEnumerator(Memory<long> data, SortedSet<int>.Enumerator enumerator, uint startCurrent = uint.MaxValue)
            {
                current = startCurrent;
                dataHandle = data.Pin();
                this.data = (long*)dataHandle.Pointer;
                length = (uint)data.Length;
                emptySlotEnumerator = enumerator;
                hasEmptySlots = emptySlotEnumerator.MoveNext();
            }

            [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                unchecked
                {
                    while (++current < length)
                    {
                        if (hasEmptySlots && emptySlotEnumerator.Current == current)
                        {
                            hasEmptySlots = emptySlotEnumerator.MoveNext();
                            continue;
                        }
                        else
                            return true;
                    }
                    return false;
                }
            }

            public void Dispose()
            {
                dataHandle.Dispose();
            }
        }

        public EntityEnumerator GetEnumerator()
        {
            return new EntityEnumerator(new Memory<long>(_indicesToUIDs, 0, _count), _emptySlots.GetEnumerator());
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        internal void CopyComponentsTo(int oldTableIndex, Table newTable, int newTableIndex)
        {
            for (int i = 0; i < _components.Length; i++)
            {
                if (Component.IsRealComponent(_components[i], _world))
                {
                    if (newTable._componentLookup.TryGetValue(_components[i], out var newTableBufferIndex))
                    {
                        var oldBuffer = _buffers[i];
                        var newBuffer = newTable._buffers[newTableBufferIndex];
                        oldBuffer.MoveTo(oldTableIndex, newBuffer, newTableIndex);
                    }
                }
            }
        }

        public bool HasComponent(int component)
        {
            return Array.IndexOf(_components, component) != -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        internal Span<T> GetComponentBuffer<T>(int component)
        {
            return (_buffers[_componentLookup[component]] as ComponentBuffer<T>).Buffer();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ComponentBuffer GetUntypedComponentBuffer(int component)
        {
            var componentIndex = Array.IndexOf(_components, component);
            if (componentIndex != -1)
            {
                return _buffers[componentIndex];
            }
            else
                return null;
        }

        internal int IndexForComponent(int component)
        {
            return _componentLookup[component];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ComponentBuffer GetUntypedComponentBufferFromIndex(int index)
        {
            return _buffers[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal long GlobalIDFromIndex(int indexInPool)
        {
            return _indicesToUIDs[indexInPool];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        internal int AddSlot(long uid)
        {
            int result = 0;
            if (_emptySlots.Count > 0)
            {
                result = _emptySlots.Min;
                _emptySlots.Remove(result);
            }
            else
            {
                result = _count++;
                for (int i = 0; i < _buffers.Length; i++)
                {
                    _buffers[i].AssureRoomFor(result + 1);
                }

                if (result >= _indicesToUIDs.Length)
                {
                    var newArray = ArrayPool<long>.Shared.Rent(_indicesToUIDs.Length * 2);
                    _indicesToUIDs.CopyTo(newArray, 0);
                    ArrayPool<long>.Shared.Return(_indicesToUIDs);
                    _indicesToUIDs = newArray;
                }
            }

            _indicesToUIDs[result] = uid;
            return result;
        }

        internal void Remove(int index)
        {
            foreach (var buffer in _buffers)
                buffer.Remove(index);

            _emptySlots.Add(index);
            _indicesToUIDs[index] = 0;
        }

        public void Sort()
        {

        }

        public void Dispose()
        {
            Dispose(false);
        }
    }
}
