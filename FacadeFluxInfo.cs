using System;
using System.Drawing;
using Grasshopper;
using Grasshopper.Kernel;

namespace FacadeFlux
{
  public class FacadeFluxInfo : GH_AssemblyInfo
  {
    public override string Name => "FacadeFlux Info";

    //Return a 24x24 pixel bitmap to represent this GHA library.
    public override Bitmap Icon => null;

    //Return a short string describing the purpose of this GHA library.
    public override string Description => "FacadeFlux is a Grasshopper toolkit for BCA Singapore facade compliance workflows (ETTV and RETV)";

    public override Guid Id => new Guid("d941b2cd-2c86-4534-a54a-5c9b4ea08a33");

    //Return a string identifying you or your company.
    public override string AuthorName => "Irfan Irwanuddin";

    //Return a string representing your preferred contact details.
    public override string AuthorContact => "irfanirwanuddin@gmail.com";

    //Return a string representing the version.  This returns the same version as the assembly.
    public override string AssemblyVersion => GetType().Assembly.GetName().Version.ToString();
  }
}