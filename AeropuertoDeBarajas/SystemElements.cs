using System;
using System.Collections.Generic;
using System.Text;

namespace AeropuertoDeBarajas
{
    class Airplane
    {
        public int currentLandingTrack { get; private set; }
        public int tasksCount { get; private set; }
        public AirplaneState State { get; private set; }
        public int PlaneNumber { get; private set; }
        public Airplane(int planeNumber)
        {
            tasksCount = 0;
            currentLandingTrack = -1;
            State = AirplaneState.Flying;
            PlaneNumber = planeNumber;
        }

        public void Land()
        {
            State = AirplaneState.Landed;
        }
        public void Landing(int landingTrack)
        {
            State = AirplaneState.Landing;
            currentLandingTrack = landingTrack;
        }
        public void TakingOff()
        {
            State = AirplaneState.TakingOff;
        }
        public void Fly()
        {
            currentLandingTrack = -1;
            State = AirplaneState.Flying;
        }
        public void Wait()
        {
            State = AirplaneState.Waiting;
        }

        public void AddTask()
        {
            tasksCount++;
        }
        public void FinishTask()
        {
            tasksCount--;
        }
    }

    enum AirplaneState
    {
        Landing,
        Landed,
        TakingOff,
        Flying,
        Waiting
    }
}
