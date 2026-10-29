namespace FontFilter;
public static class PhysicalPageScale
{
 public const double DipPerMillimeter=96/25.4;
 public static double FromDisplay(double pixelWidth,double scaling,double widthMillimeters)
 {
  if(!double.IsFinite(pixelWidth)||!double.IsFinite(scaling)||!double.IsFinite(widthMillimeters)||pixelWidth<=0||scaling<=0||widthMillimeters<=0)throw new ArgumentOutOfRangeException(nameof(widthMillimeters));
  return pixelWidth/scaling/widthMillimeters/DipPerMillimeter;
 }
 public static double Correct(double currentCorrection,double measuredMillimeters,double referenceMillimeters=100)
 {
  if(!double.IsFinite(currentCorrection)||!double.IsFinite(measuredMillimeters)||currentCorrection<=0||measuredMillimeters<=0)throw new ArgumentOutOfRangeException(nameof(measuredMillimeters));
  return Math.Clamp(currentCorrection*referenceMillimeters/measuredMillimeters,.25,4);
 }
}
