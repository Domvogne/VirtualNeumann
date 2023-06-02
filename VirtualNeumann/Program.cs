using System.Text;

namespace VirtualNeumann
{
    public class Program
    {
        static void Main(string[] args)
        {
            Computer vvn = new Computer(50);
            //vvn.SetProgram("TestCode.hasm");
            //vvn.Run();
            Memory testMem = new Memory(0, 256);
            Random random = new Random();
            for (int i = 0; i < 256; i++)
            {
                testMem.Data[i] = (short)random.Next(0, 150);
            }
            Console.WriteLine(testMem.ToString());
        }
    }

    public class Computer
    {

        public static (string code, byte args)[] Commands;
        public ArithmeticLogicUnit ALU;
        public ControlUnit CU;
        public Memory Cache;
        public Memory RAM;
        public InputOutput IO;
        public string[] Programm;
        public Computer(int tick)
        {
            ALU = new ArithmeticLogicUnit();
            CU = new ControlUnit();

            Cache = new Memory(tick, 512);
            RAM = new Memory(tick * 3, 1024);

            IO = new InputOutput();

            ALU.IO = IO;
            ALU.CU = CU;
            ALU.RAM = RAM;
            ALU.Cache = Cache;

            CU.ALU = ALU;
            CU.Cache = Cache;
            CU.RAM = RAM;
        }

        public void SetProgram(string progFile)
        {
            Programm = File.ReadAllLines(progFile);
        }

        public void Run()
        {
            CU.SetMachineCode(Compile());
            CU.Run();
        }

        public void LoadCommands()
        {
            var allCommands = File.ReadAllLines("Commands.txt");
            Commands = new (string code, byte args)[allCommands.Length];
            for (int i = 0; i < allCommands.Length; i++)
            {
                var cmd = allCommands[i].Split(' ');
                Commands[i] = (cmd[0], Convert.ToByte(cmd[1]));
            }
        }

        public short[] Compile()
        {
            List<short> mc = new List<short>();
            var cmds = Computer.Commands.ToList();
            foreach (var line in Programm)
            {
                var parts = line.Split(' ');
                mc.Add((short)cmds.FindIndex(i => i.code == parts[0]));
                for (int i = 1; i < parts.Length; i++)
                {
                    mc.Add(Convert.ToInt16(parts[i]));
                }
            }
            return mc.ToArray();
        }
    }
    public class ArithmeticLogicUnit
    {
        public ControlUnit CU;
        public Memory Cache;
        public Memory RAM;
        public InputOutput IO;
        public void Execute(short[] args)
        {
            var ops = args[1..];
            switch (args[0])
            {
                case 0:
                    short buffer;
                    Memory m = ops[1] switch { 0 => Cache, 1 => RAM };
                    buffer = m.Get(ops[1]);
                    m.Set(ops[1], m.Get(ops[2]));
                    m.Set(ops[2], buffer);
                    break;
                case 1:
                    buffer = Cache.Get(ops[0]);
                    Cache.Set(ops[0], RAM.Get(ops[1]));
                    RAM.Set(ops[1], buffer);
                    break;
                case 2:
                    Cache.Set(ops[0], IO.In());
                    break;
                case 3:
                    IO.Out(Cache.Get(ops[0]));
                    break;
                case 4:
                    Cache.Set(ops[0], (short)(Cache.Get(ops[0]) + Cache.Get(ops[1])));
                    break;
                case 5:
                    Cache.Set(ops[0], (short)(Cache.Get(ops[0]) - Cache.Get(ops[1])));
                    break;
                case 6:
                    Cache.Set(ops[0], (short)(Cache.Get(ops[0]) * Cache.Get(ops[1])));
                    break;
                case 7:
                    Cache.Set(ops[0], (short)(Cache.Get(ops[0]) / Cache.Get(ops[1])));
                    break;
                case 8:
                    Cache.Set(ops[0], (short)(Cache.Get(ops[1]) == Cache.Get(ops[2]) ? 1 : 0));
                    break;
                case 9:
                    Cache.Set(ops[0], (short)(Cache.Get(ops[0]) + 1));
                    break;
                case 10:
                    Cache.Set(ops[0], (short)(Cache.Get(ops[0]) - 1));
                    break;
                case 11:
                    CU.InstructionPointer = RAM.Get(ops[0]);
                    break;
                case 12:
                    CU.InstructionPointer = -1;
                    break;
                case 13:
                    Cache.Set(ops[0], ops[1]);
                    break;
                default:
                    break;
            }
        }
    }
    public class ControlUnit
    {
        public short InstructionPointer;
        public Memory Cache;
        public Memory RAM;
        public ArithmeticLogicUnit ALU;

        public void SetMachineCode(short[] program)
        {
            program.CopyTo(RAM.Data, 0);
        }

        public void Run()
        {

            InstructionPointer = -1;
            do
            {
                InstructionPointer++;
                var op = RAM.Get(InstructionPointer);

                var args = new short[Computer.Commands[op].args];
                for (short i = 0; i < args.Count(); i++)
                {
                    args[i] = RAM.Get(++InstructionPointer);
                }
                ALU.Execute(args);
            }
            while (InstructionPointer != -1);

        }
    }
    public class Memory
    {
        int lastChange;
        public int Delay;
        public short[] Data;
        public Memory(int speed, int size)
        {
            Delay = speed;
            Data = new short[size];
        }

        public short Get(short index)
        {
            Thread.Sleep(Delay);
            return Data[index];
        }

        public void Set(short index, short value)
        {
            Thread.Sleep(Delay);
            Data[index] = value;
            lastChange = index;
        }

        public override string ToString()
        {
            var stringed = Data.Select(x => x.ToString()).ToList();
            var mLen = stringed.Max(x => x.Length);
            StringBuilder sb = new StringBuilder();
            var lines = stringed.Chunk(16);
            int n = 0;
            foreach (var line in lines)
            {
                foreach (var cell in line)
                {

                    sb.Append(cell + string.Join("", Enumerable.Repeat(' ', mLen - cell.Length)) + (n == lastChange ? "#" : " ") + "|");
                    n++;
                }
                sb.AppendLine('\n' + string.Join("", Enumerable.Repeat('-', (mLen + 2) * Data.Length / 16)));
            }
            return sb.ToString();
        }
    }
    public class InputOutput
    {
        public void Out(short value)
        {
            Console.WriteLine(value);
        }
        public short In()
        {
            return Convert.ToInt16(Console.ReadLine(), 16);
        }
    }
}