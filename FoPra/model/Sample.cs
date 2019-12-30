namespace FoPra.model
{ 
	public class Sample
	{
		public double TotalDiameter { get; }
		public double CellThickness { get; }
		public double MuSample { get; }
		public double MuCell { get; }
		public double DetectorDistance { get; }
		public (double, double) DetectorOffset { get; }

		public Sample(
			double totalDiameter, 
			double cellThickness, 
			double muSample, 
			double muCell, 
			double detectorDistance, 
			(double, double) detectorOffset
			) 
		{ 
			TotalDiameter = totalDiameter;
			CellThickness = cellThickness;
			MuSample = muSample; 
			MuCell = muCell; 
			DetectorDistance = detectorDistance; 
			DetectorOffset = detectorOffset;
		}

		public override string ToString()
		{
			return "Sample:   ["+
			       $"dia={TotalDiameter}mm, thickness={CellThickness}mm, " +
			       $"µ_S={MuSample}cm^-1, µ_C={MuCell}cm^-1, dist={DetectorDistance}mm, offset={DetectorOffset}mm]";
		}
	}
}
