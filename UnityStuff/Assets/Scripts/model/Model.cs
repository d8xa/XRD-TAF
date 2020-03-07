namespace FoPra.model
{
  public class Model
  {
    //enum Modes {_1D, _2D}  // to switch between use cases and discard unused fields. TODO: integrate. 
    public enum Modes {
      Point,Area,Integrated
    }
    public enum AbsorbtionType {
      All,Cell,Sample,CellAndSample
    }
    public enum StrahlProfil {
      Oval,Rechteck
    }

    private Settings settings;
    private DetektorSettings detector;
    private SampleSettings sample;
    private RaySettings ray;


    public Model(Settings settings, DetektorSettings detector, SampleSettings sample, RaySettings ray) {
      this.settings = settings;
      this.detector = detector;
      this.sample = sample;
      this.ray = ray;
    }
  }
}
