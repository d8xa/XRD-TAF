namespace FoPra.model
{
  class Model
  {
    enum Modes {_1D, _2D}  // to switch between use cases and discard unused fields. TODO: integrate. 
    
    public string Name { get; set; }
    public string Mode { get; }
    public Detector Detector { get; }
    public Sample Sample { get; }

    public Model(Detector detector, Sample sample, string name)
    {
      Name = name;
      Detector = detector;
      Sample = sample;
    }
    
    public Model(Detector detector, Sample sample)
    {
      Detector = detector;
      Sample = sample;
    }

    public override string ToString()
    {
      if (Name is null)
      {
        return $"Model:\n\t{Detector}\n\t{Sample}";
      }
      return $"Model (\"{Name}\"):\n\t{Detector}\n\t{Sample}";
    }
  }
}
