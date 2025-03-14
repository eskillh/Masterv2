using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Masterv2
{
    public static class MaterialList
    {
        public static List<double> GetMaterial(string material)
        {
            var materialprop = new List<double>();
            if (material == "C14")
            {
                materialprop.Add(0.72); //ft [kN/cm^2]
                materialprop.Add(1.6); //fc [kNcm^2]
                materialprop.Add(14); //fm [N/mm^2]
                materialprop.Add(3.0); //fv [N/mm^2]
                materialprop.Add(4.7); //E0.05 [kN/mm^2]
                materialprop.Add(0.44); //Gmean [kN/mm^2]
            }
            if (material == "C16")
            {
                materialprop.Add(0.85); //ft [kN/cm^2]
                materialprop.Add(1.7); //fc [kNcm^2]
                materialprop.Add(16); //fm [N/mm^2]
                materialprop.Add(3.2); //fv [N/mm^2]
                materialprop.Add(5.4); //E0.05 [kN/mm^2]
                materialprop.Add(0.50); //Gmean [kN/mm^2]
            }
            if (material == "C18")
            {
                materialprop.Add(1); //ft [kN/cm^2]
                materialprop.Add(1.8); //fc [kNcm^2]
                materialprop.Add(18); //fm [N/mm^2]
                materialprop.Add(3.4); //fv [N/mm^2]
                materialprop.Add(6.0); //E0.05 [kN/mm^2]
                materialprop.Add(0.56); //Gmean [kN/mm^2]
            }
            if (material == "C20")
            {
                materialprop.Add(1.15); //ft [kN/cm^2]
                materialprop.Add(1.9); //fc [kNcm^2]
                materialprop.Add(20); //fm [N/mm^2]
                materialprop.Add(3.6); //fv [N/mm^2]
                materialprop.Add(6.4); //E0.05 [kN/mm^2]
                materialprop.Add(0.59); //Gmean [kN/mm^2]
            }
            if (material == "C22")
            {
                materialprop.Add(1.3); //ft [kN/cm^2]
                materialprop.Add(2); //fc [kNcm^2]
                materialprop.Add(22); //fm [N/mm^2]
                materialprop.Add(3.8); //fv [N/mm^2]
                materialprop.Add(6.7); //E0.05 [kN/mm^2]
                materialprop.Add(0.63); //Gmean [kN/mm^2]
            }
            if (material == "C24")
            {
                materialprop.Add(1.45); //ft [kN/cm^2]
                materialprop.Add(2.1); //fc [kNcm^2]
                materialprop.Add(24); //fm [N/mm^2]
                materialprop.Add(4.0); //fv [N/mm^2]
                materialprop.Add(7.4); //E0.05 [kN/mm^2]
                materialprop.Add(0.69); //Gmean [kN/mm^2]
            }
            if (material == "C27")
            {
                materialprop.Add(1.65); //ft [kN/cm^2]
                materialprop.Add(2.2); //fc [kNcm^2]
                materialprop.Add(27); //fm [N/mm^2]
                materialprop.Add(4.0); //fv [N/mm^2]
                materialprop.Add(7.7); //E0.05 [kN/mm^2]
                materialprop.Add(0.72); //Gmean [kN/mm^2]
            }
            if (material == "C30")
            {
                materialprop.Add(1.9); //ft [kN/cm^2]
                materialprop.Add(2.4); //fc [kNcm^2]
                materialprop.Add(30); //fm [N/mm^2]
                materialprop.Add(4.0); //fv [N/mm^2]
                materialprop.Add(8.0); //E0.05 [kN/mm^2]
                materialprop.Add(0.75); //Gmean [kN/mm^2]
            }
            if (material == "C35")
            {
                materialprop.Add(2.25); //ft [kN/cm^2]
                materialprop.Add(2.5); //fc [kNcm^2]
                materialprop.Add(35); //fm [N/mm^2]
                materialprop.Add(4.0); //fv [N/mm^2]
                materialprop.Add(8.7); //E0.05 [kN/mm^2]
                materialprop.Add(0.81); //Gmean [kN/mm^2]
            }
            if (material == "C40")
            {
                materialprop.Add(2.6); //ft [kN/cm^2]
                materialprop.Add(2.7); //fc [kNcm^2]
                materialprop.Add(40); //fm [N/mm^2]
                materialprop.Add(4.0); //fv [N/mm^2]
                materialprop.Add(9.4); //E0.05 [kN/mm^2]
                materialprop.Add(0.88); //Gmean [kN/mm^2]
            }
            if (material == "C45")
            {
                materialprop.Add(3.0); //ft [kN/cm^2]
                materialprop.Add(2.9); //fc [kNcm^2]
                materialprop.Add(45); //fm [N/mm^2]
                materialprop.Add(4.0); //fv [N/mm^2]
                materialprop.Add(10.1); //E0.05 [kN/mm^2]
                materialprop.Add(0.94); //Gmean [kN/mm^2]
            }
            if (material == "C50")
            {
                materialprop.Add(3.35); //ft [kN/cm^2]
                materialprop.Add(3.0); //fc [kNcm^2]
                materialprop.Add(50); //fm [N/mm^2]
                materialprop.Add(4.0); //fv [N/mm^2]
                materialprop.Add(10.7); //E0.05 [kN/mm^2]
                materialprop.Add(1.00); //Gmean [kN/mm^2]
            }

            return materialprop;
        }
    }
}
