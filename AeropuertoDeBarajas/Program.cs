using System;

namespace AeropuertoDeBarajas
{
    class Program
    {
        static void Main(string[] args)
        {
            var simulation = new SimulationSystem(5, 10);
            simulation.StartSimulation();
        }
    }
}
