using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace SunriseSunset
{
    class Program
    {
        static void Main(string[] args)
        {
            Location test = new Location();

            test.setLongitude(139.691706);  // Tokyo, Japan
            test.setLatitude(35.689487);  

            calculateSunriseSunset(test);

            //Console.WriteLine("Sunrise: {0}   Sunset: {1}", test.getSunrise(), test.getSunset());
        }

        static void calculateSunriseSunset(Location a)
        {
            double lng = a.getLongitude();
            double lat = a.getLatitude();
            double n = julianCycle(lng);
            double j = approxJulianDateSolarNoon(n, lng);
            double _meanAnomally = meanAnomally(j);
            double center = equationOfCenter(_meanAnomally);
            double eLNG = eclipticalLongitude(_meanAnomally, center);
            double jTransit = julianTransit(j,_meanAnomally,eLNG);
            double _sunDeclination = sunDeclination(eLNG);
            double _hourAngle = hourAngle(lat, _sunDeclination);

            // Recalculate j, using hourAngle
            j = approxJulianDateSolarNoon(n,_hourAngle+lng);

            // Calculate sunrise/sunset in Julian date/time
            double _sunset = sunset(j, _meanAnomally, eLNG); 
            double _sunrise = sunrise(jTransit, _sunset);

            // Convert Julian Date to readable calendar date/time

            // Need to add/subtract time zone difference for calculation (Ex: Tokyo sunrise +9 = local sunrise)
            // Console.WriteLine("Local offset from UTC time: ");
            // int offset = Convert.ToInt32(Console.ReadLine()); 

            // Set sunrise/sunset time of location object
            a.setSunrise(_sunrise);
            a.setSunset(_sunset);
           Console.WriteLine("Sunrise:  {0}   \nSunset:   {1}",_sunrise, _sunset);
        }

        static double approxJulianDateSolarNoon(double n, double lng)
        {
            return (2451545 + 0.0009 + (lng/360) + n);
        }

        static double eclipticalLongitude(double m, double c)
        {
            return ((m + 102.9372 + c + 180) % 360);
        }

        static double equationOfCenter(double m)
        {
            m = m * (Math.PI / 180);
            return ((1.9148 * Math.Sin(m)) + (0.0200 * Math.Sin(2 * m)) + (0.0003 * Math.Sin(3 * m)));
        }

        static double hourAngle(double lat, double sD)
        {
            // Convert to radians
            lat = lat * (Math.PI / 180);
            sD = sD * (Math.PI / 180);

            double a = Math.Sin(-0.83 * (Math.PI/180)) - Math.Sin(lat) * Math.Sin(sD);            
            double b = Math.Cos(lat) * Math.Cos(sD);

            // Calculate and convert back to degrees
            double h = Math.Acos(a/b) * (180/Math.PI); 

            return h;
        }

        static double julianCycle(double longitude)
        {
            double n = (julianDate() - 2451545 - 0.0009) - (longitude / 360); //0.0009: date and time of a transit of the sun
            return Math.Round(n);
        }

        static double julianDate()
        {
            Console.Write("Month: ");
            int month = Convert.ToInt32(Console.ReadLine());

            Console.Write("Day: ");
            int day = Convert.ToInt32(Console.ReadLine());

            Console.Write("Year: ");
            int year = Convert.ToInt32(Console.ReadLine());

            double N1 = 275 * month / 9;
            double N2 = (month + 9) / 12;
            double N = N1 - (N2 * (1 + 2 / 3)) + day - 30;

            int yearDays = yearsToDays(2000, year);
            N = yearDays + N + 2451545;
            return N;
        }

        static double julianTransit(double _j, double m, double lng)
        {
            m = m * (Math.PI / 180);
            lng = lng * (Math.PI / 180);
            double _jTransit = _j + (0.0053 * Math.Sin(m)) - (0.0069 * Math.Sin(2 * lng));
            return _jTransit;
        }

        static double meanAnomally(double _j)
        {
            return  ((357.5291 + 0.98560028 * (_j - 2451545)) % 360);
        }

        static double sunDeclination(double lng)
        {
            lng = lng * (Math.PI / 180);
            double a = Math.Sin(lng);
            double b = Math.Sin((23.45 * Math.PI)/180);
            double c = Math.Asin(a * b);
            c = c * 180 / Math.PI; // Converts back to degrees
            return c;
        }

        static double sunset(double _j, double m, double eLNG)
        {
            m = m * Math.PI / 180;
            eLNG = eLNG * Math.PI / 180;

            double a = _j + (0.0053 * Math.Sin(m)) - ((0.0069) * Math.Sin(2 * eLNG));
            return a;
        }

        static double sunrise(double jTransit, double jSet)
        {
            return (jTransit - (jSet - jTransit));
        }

        static int yearsToDays(int start, int end)
        {
            int _leapYears = leapYears(start, end);
            return ((end - start) * 365 + _leapYears);

        }

        static int leapYears(int start, int end)
        {
            int _leapYears = 0;
            for (int i = start; i < end; i++)
            {
                if ((i % 4 == 0) & (i % 100 != 0))
                {
                    _leapYears++;
                }
            }
            return _leapYears;
        }

        class Location
        {
            private static double latitude;
            private static double longitude;
            private static double sunrise;
            private static double sunset;

            #region Setters

            public void setLatitude(double _latitude)
            {
                latitude = _latitude;
            }

            public void setLongitude(double _longitude)
            {
                longitude = _longitude;
            }

            public void setSunrise(double _sunrise)
            {
                sunrise = _sunrise;
            }

            public void setSunset(double _sunset)
            {
                sunset = _sunset;
            }

            #endregion

            #region Getters

            public double getLatitude()
            {
                return latitude;
            }

            public double getLongitude()
            {
                return longitude;
            }

            public double getSunrise()
            {
                return sunrise;
            }

            public double getSunset()
            {
                return sunset;
            }

            #endregion
        }
    }
} 