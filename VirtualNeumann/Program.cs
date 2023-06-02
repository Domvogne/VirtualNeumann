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
            Computer.LoadCommands();
            Computer c = new Computer(50);

            c.SetProgram("C:\\Users\\Mimm\\Projects\\VisualStudioProjects\\VirtualNeumann\\VirtualNeumann\\TestCode.hasm");
            c.Run();

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
        public bool UserTick;
        public Computer(int tick, bool clickMode = false)
        {
            ALU = new ArithmeticLogicUnit();
            CU = new ControlUnit();

            Cache = new Memory(tick, 128);
            RAM = new Memory(tick * 3, 1024);

            IO = new InputOutput();

            ALU.IO = IO;
            ALU.CU = CU;
            ALU.RAM = RAM;
            ALU.Cache = Cache;

            CU.ALU = ALU;
            CU.Cache = Cache;
            CU.RAM = RAM;
            
            UserTick = clickMode;
        }

        public void SetProgram(string progFile)
        {
            Programm = File.ReadAllLines(progFile);
        }

        public void Run()
        {
            CU.SetMachineCode(Compile());
            CU.Run(UserTick);
        }

        public static void LoadCommands()
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
                var clear = line.Split("#")[0];
                var parts = clear.Trim().Split(' ');
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
                    CU.InstructionPointer = ops[0];
                    break;
                case 12:
                    CU.InstructionPointer = -1;
                    break;
                case 13:
                    Cache.Set(ops[0], ops[1]);
                    break;
                case 14:
                    Cache.Set(ops[0], (short)(Cache.Get(ops[0]) == 0 ? 1 : 0));
                    break;
                case 15:
                    if (Cache.Get(ops[0]) != 0)
                        CU.InstructionPointer = ops[1];
                    break;
                case 16:
                    if (Cache.Get(ops[0]) == 0)
                        CU.InstructionPointer = ops[1];
                    break;
                case 17:
                    Cache.Set(ops[1], Cache.Get(ops[0]));
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
        int codeLen;
        public void SetMachineCode(short[] program)
        {
            codeLen = program.Length;
            Cache.DataOffset = (codeLen / 16 + 1) * 16;
            program.CopyTo(Cache.Data, 0);
        }

        public void Run(bool click = false)
        {

            InstructionPointer = 0;
            do
            {
                if (click)
                    Console.ReadKey();
                var op = Cache.Get(InstructionPointer, true);
                Cache.RunIndex = InstructionPointer;
                
                var args = new short[Computer.Commands[op].args + 1];
                args[0] = op;
                for (short i = 1; i < args.Count(); i++)
                {
                    args[i] = Cache.Get(++InstructionPointer, true);
                }

                ALU.Execute(args);
                InstructionPointer++;
                Console.SetCursorPosition(0, 0);
                Console.Clear();
                Console.WriteLine(Cache.ToString());
            }
            while (InstructionPointer != -1);

        }
    }
    public class Memory
    {
        int lastChange;
        public int Delay;
        public short[] Data;
        public int DataOffset;
        public int RunIndex = -1;
        public Memory(int speed, int size)
        {
            Delay = speed;
            Data = new short[size];
        }

        public short Get(short index, bool code = false)
        {
            Thread.Sleep(Delay);
            return Data[index + (code ? 0 : DataOffset)];
        }

        public void Set(short index, short value, bool code = false)
        {
            Thread.Sleep(Delay);
            Data[index + (code ? 0 : DataOffset)] = value;
            lastChange = index + (code ? 0 : DataOffset);
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

                    sb.Append(RunIndex == n ? ">" : " ");
                    sb.Append(cell);
                    sb.Append(string.Join("", Enumerable.Repeat(' ', mLen - cell.Length)));
                    sb.Append((n == lastChange ? "#" : " ") + "|");
                    n++;
                }
                sb.AppendLine("\n");
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
            Console.Write(">");
            var pre = Console.ReadLine();
            return Convert.ToInt16(pre);
        }
    }
}