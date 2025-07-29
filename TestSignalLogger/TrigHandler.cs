namespace TestSignalLogger
{
    [Serializable]
    public class TrigHandler
    {
        public event TrigEventHandler TrigActivated;

        public void AddNewSample(List<double> l, double value, Trig trig, int index = -1)
        {
            if (index >= 0)
            {
                l.Insert(index, value);
            }
            else
            {
                l.Add(value);
            }

            if (trig != null && trig.Type != TrigType.No && trig.Activated(value) && this.TrigActivated != null)
            {
                TrigEventArgs e = new TrigEventArgs
                {
                    StopTime = trig.PostTime,
                    RestartLog = trig.RestartLog
                };
                this.TrigActivated(this, e);
            }
        }
    }
}
