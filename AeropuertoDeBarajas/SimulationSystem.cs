using System;
using System.Collections.Generic;
using System.Text;

namespace AeropuertoDeBarajas
{
    public class SimulationSystem
    {
        Tuple<bool, int, List<string>, double, double, double>[] tracks;
        List<Airplane> waiting;
        List<Tuple<Airplane, double, string>> actions;
        private long lastUniform;
        private long a = 16807, m;
        private double simulationTime;
        private int landingTracks;
        private bool landingTracksFree;
        private int tracksFree;
        
        public SimulationSystem(int landingTracks, int planes)
        {
            tracks = new Tuple<bool, int, List<string>, double, double, double>[landingTracks];
            waiting = new List<Airplane>();
            landingTracksFree = true;
            tracksFree = landingTracks;
            simulationTime = 0;
            this.landingTracks = landingTracks;
            CalculateM();
            SetSeed();
            InitializePlanes(planes);
        }

        public void StartSimulation()
        {
            PrintSimulationStep();
            while(actions.Count > 0 && simulationTime + actions[0].Item2 < 1000080)
            {
                simulationTime = simulationTime + actions[0].Item2;
                Console.WriteLine("  {0}     {1}",actions.Count, simulationTime);
                switch (actions[0].Item1.State)
                {
                    case AirplaneState.Flying:
                        {
                            if (landingTracksFree)
                            {
                                for (int i = 0; i < landingTracks; i++)
                                {
                                    if (tracks[i].Item1)
                                    {
                                        tracksFree--;
                                        if (tracksFree == 0)
                                        {
                                            landingTracksFree = false;
                                        }
                                        double freeTime = tracks[i].Item4 + (simulationTime - tracks[i].Item5);
                                        tracks[i] = new Tuple<bool, int, List<string>, double, double, double>(false, actions[0].Item1.PlaneNumber, new List<string> { "Aterrizando" }, freeTime, -1, -1);
                                        actions[0].Item1.Landing(i);
                                        actions.Add(new Tuple<Airplane, double, string>(actions[0].Item1, simulationTime + GenerateNormal(10, 5), ""));
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                waiting.Add(actions[0].Item1);
                            }
                            break;
                        }
                    case AirplaneState.Landing:
                        {
                            ChangeTrackState(actions[0].Item1.currentLandingTrack, "Aterrizando");
                            actions[0].Item1.Land();
                            List<string> tasks = new List<string> { "Llenando Combustible" };
                            AddTrackState(actions[0].Item1.currentLandingTrack, "Llenando Combustible");
                            actions[0].Item1.AddTask();
                            actions.Add(new Tuple<Airplane, double, string>(actions[0].Item1, simulationTime + GenerateExponential(30), "Llenando Combustible"));
                            if (LoadingOrUnloading())
                            {
                                tasks.Add("Cargando");
                                AddTrackState(actions[0].Item1.currentLandingTrack, "Cargando");
                                actions.Add(new Tuple<Airplane, double, string>(actions[0].Item1, simulationTime + GenerateExponential(30), "Cargando"));
                                actions[0].Item1.AddTask();
                            }
                            break;
                        }
                    case AirplaneState.Landed:
                        {
                            List<string> currentTask = new List<string>();
                            if (actions[0].Item3 == "Llenando Combustible")
                            {
                                actions[0].Item1.FinishTask();
                                ChangeTrackState(actions[0].Item1.currentLandingTrack, "Llenando Combustible");
                            }
                            if (actions[0].Item3 == "Cargando")
                            {
                                actions[0].Item1.FinishTask();
                                ChangeTrackState(actions[0].Item1.currentLandingTrack, "Cargando");
                                if (LoadingOrUnloading())
                                {
                                    actions.Add(new Tuple<Airplane, double, string>(actions[0].Item1, simulationTime + GenerateExponential(30), "Descargando"));
                                    AddTrackState(actions[0].Item1.currentLandingTrack, "Descargando");
                                    actions[0].Item1.AddTask();
                                }
                            }
                            if (actions[0].Item3 == "Descargando")
                            {
                                actions[0].Item1.FinishTask();
                                ChangeTrackState(actions[0].Item1.currentLandingTrack, "Descargando");
                            }
                            if (actions[0].Item3 == "Reparando")
                            {
                                actions[0].Item1.FinishTask();
                                ChangeTrackState(actions[0].Item1.currentLandingTrack, "Reparando");
                            }
                            if (actions[0].Item1.tasksCount == 0)
                            {
                                if (Repair())
                                {
                                    actions.Add(new Tuple<Airplane, double, string>(actions[0].Item1, simulationTime + GenerateExponential(30), "Reparando"));
                                    AddTrackState(actions[0].Item1.currentLandingTrack, "Reparando");
                                    actions[0].Item1.AddTask();
                                }
                                else
                                {
                                    actions.Add(new Tuple<Airplane, double, string>(actions[0].Item1, simulationTime + GenerateNormal(10, 5), ""));
                                    AddTrackState(actions[0].Item1.currentLandingTrack, "Despegando");
                                    actions[0].Item1.TakingOff();
                                }
                            }
                            break;
                        }
                    case AirplaneState.TakingOff:
                        {
                            actions.Add(new Tuple<Airplane, double, string>(actions[0].Item1, simulationTime + GenerateExponential(30), ""));
                            tracks[actions[0].Item1.currentLandingTrack] = new Tuple<bool, int, List<string>, double, double, double>(true, -1, new List<string>(), tracks[actions[0].Item1.currentLandingTrack].Item4, simulationTime, -1);
                            tracksFree++;
                            landingTracksFree = true;
                            actions[0].Item1.Fly();
                            if (waiting.Count > 0)
                            {
                                actions[0] = new Tuple<Airplane, double, string>(waiting[0], simulationTime, "");
                                waiting.RemoveAt(0);
                                PrintSimulationStep();
                                continue;
                            }
                            break;
                        }
                    default:
                        break;
                }
                actions.RemoveAt(0);
                PrintSimulationStep();
                SortActionsList();
            }
        }

        private void PrintSimulationStep()
        {
            Console.WriteLine("AEROPUERTO DE BARAJAS\n");
            for (int i = 0; i < tracks.Length; i++)
            {
                if (tracks[i].Item1)
                {
                    Console.WriteLine("PISTA {0}: Libre     Tiempo: {1}\n", i+1, tracks[i].Item4);
                }
                else
                {
                    string tasks = "Acción: ";
                    foreach (var item in tracks[i].Item3)
                    {
                        tasks = tasks + item + "  ";
                    }
                    Console.WriteLine("PISTA {0}: Ocupada    Avión: {1}   {2}    Tiempo:{3}\n", i+1, tracks[i].Item2+1, tasks, tracks[i].Item4);
                }
            }
            Console.Write("Aviones en Espera: ");
            for (int i = 0; i < waiting.Count; i++)
            {
                Console.Write("Avion {0}    ", waiting[i].PlaneNumber+1);
            }
            Console.ReadLine();
            Console.Clear();
        }

        private void ChangeTrackState(int pos, string taskToRemove)
        {
            tracks[pos] = new Tuple<bool, int, List<string>, double, double, double>(tracks[pos].Item1, tracks[pos].Item2, RemoveStateFromList(tracks[pos].Item3, taskToRemove), tracks[pos].Item4, tracks[pos].Item5, tracks[pos].Item6);
        }
        private void AddTrackState(int pos, string taskToRemove)
        {
            tracks[pos] = new Tuple<bool, int, List<string>, double, double, double>(tracks[pos].Item1, tracks[pos].Item2, AddStateFromList(tracks[pos].Item3, taskToRemove), tracks[pos].Item4, tracks[pos].Item5, tracks[pos].Item6);
        }
        private List<string> RemoveStateFromList(List<string> currenTasks, string taskToRemove)
        {
            List<string> result = new List<string>();
            for (int i = 0; i < currenTasks.Count; i++)
            {
                if (currenTasks[i] != taskToRemove)
                {
                    result.Add(currenTasks[i]);
                }
            }
            return result;
        }
        private List<string> AddStateFromList(List<string> currenTasks, string taskToAdd)
        {
            List<string> result = new List<string>();
            for (int i = 0; i < currenTasks.Count; i++)
            {
                result.Add(currenTasks[i]);
            }
            result.Add(taskToAdd);
            return result;
        }

        private void SortActionsList()
        {
            double min = double.MaxValue;
            int pos = 0;
            for (int i = 0; i < actions.Count; i++)
            {
                if (actions[i].Item2 < min)
                {
                    min = actions[i].Item2;
                    pos = i;
                }
            }
            Tuple<Airplane, double, string> temp = new Tuple<Airplane, double, string>(actions[0].Item1, actions[0].Item2, actions[0].Item3);
            actions[0] = new Tuple<Airplane, double, string>(actions[pos].Item1, actions[pos].Item2, actions[pos].Item3);
            actions[pos] = new Tuple<Airplane, double, string>(temp.Item1, temp.Item2, temp.Item3);
        }

        private void InitializePlanes(int planes)
        {
            actions = new List<Tuple<Airplane, double, string>>();
            double alpha = 30;
            for (int i = 0; i < planes; i++)
            {
                actions.Add(new Tuple<Airplane, double, string>(new Airplane(i), GenerateExponential(alpha), ""));
            }
            SortActionsList();
            for (int i = 0; i < tracks.Length; i++)
            {
                tracks[i] = new Tuple<bool, int, List<string>, double, double, double>(true, -1, new List<string>(), 0, 0, -1);
            }
        }



        private void CalculateM()
        {
            long result = 1;

            for (int i = 0; i < 31; i++)
            {
                result *= 2;
            }

            m = result - 1;
        }
        private void SetSeed()
        {
            Random seed = new Random();
            lastUniform = seed.Next(0, (int)m);
        }
        private double GenerateNormal(double alpha, double N)
        {
            Random rand = new Random();
            double u1 = rand.NextDouble(); //these are uniform(0,1) random doubles
            double u2 = rand.NextDouble(); 
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2); //random normal(0,1) 
            double randNormal = alpha + N * randStdNormal; //random normal(mean,stdDev^2)
            return randNormal;

        }
        private double GenerateUniform()
        {
            lastUniform = (a * lastUniform) % m;
            return (double)lastUniform / m;
        }
        private bool LoadingOrUnloading()
        {
            Random rand = new Random();
            int result = rand.Next(0, 2);
            if (result == 0)
            {
                return true;
            }
            return false;
        }
        private bool Repair()
        {
            Random rand = new Random();
            int result = rand.Next(0, 10);
            if (result == 0)
            {
                return true;
            }
            return false;
        }
        private double GenerateExponential(double alpha)
        {
            double uniform_variable;
            do
            {
                uniform_variable = GenerateUniform();
            } while (uniform_variable == 0d); // cannot choose 0 because of the log operator
            return (-Math.Log(uniform_variable)) / alpha;
        }
    }
}
