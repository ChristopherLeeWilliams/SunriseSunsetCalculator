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
            Location test;

            while(true)
            {
            // Run Operations
            test = createLocation();
            calculateSunriseSunset(test);

            // Asks user to repeat
            Console.WriteLine("Would you like to use another location? (y/n)");
            if (Console.ReadLine() != "y" || Console.ReadLine() != "Y") { break; }
            Console.WriteLine("\n");
            }
    
        }

        static void adjustOffset(double[] a, double[] b)
        {
            Console.WriteLine("\nLocal offset to UTC time: (Ex: California +8, Tokyo -9)");
            int offset = Convert.ToInt32(Console.ReadLine());

            // Sunrise:
            a[3] = a[3] + 24 - offset; // Add 24 to local hour to prevent negative hours after calculating offset
            a[1]--;                    // Subtract 1 day, to adjust for adding 24 hours

            while (a[3] >= 24)         // While the hours are still greater than or equal to 24
            {
                a[3] -= 24;            // subtract 24 hours
                a[1]++;                // and add 1 to day
            }

            // Sunset: 
            b[3] = b[3] + 24 - offset;
            b[1]--;

            while (b[3] >= 24)
            {
                b[3] -= 24;
                b[2]++;
            }

        }

        static double approxJulianDateSolarNoon(double n, double lng)
        {
            return (2451545 + 0.0009 + (lng/360) + n);
        }

        static void calculateSunriseSunset(Location a)
        {
            double lng = a.Longitude;
            double lat = a.Latitude;
            double n = julianCycle(lng);
            double j = approxJulianDateSolarNoon(n, lng);
            double _meanAnomally = meanAnomally(j);
            double center = equationOfCenter(_meanAnomally);
            double eLNG = eclipticalLongitude(_meanAnomally, center);
            double jTransit = julianTransit(j, _meanAnomally, eLNG);
            double _sunDeclination = sunDeclination(eLNG);
            double _hourAngle = hourAngle(lat, _sunDeclination);

            // Recalculate j, using hourAngle
            j = approxJulianDateSolarNoon(n, _hourAngle + lng);

            // Calculate sunrise/sunset in Julian date/time
            double _sunset = sunset(j, _meanAnomally, eLNG);
            double _sunrise = sunrise(jTransit, _sunset);

            // Set sunrise/sunset time of location object
            a.Sunrise = julianToCalendar(_sunrise);
            a.Sunset = julianToCalendar(_sunset);

            // Adjust for local offset from UTC time
            adjustOffset(a.Sunrise, a.Sunset);

            // Print the adjust times
            printSunriseSunset(a.Sunrise, a.Sunset);
        }

        static Location createLocation()
        {
            Location test = new Location();

            Console.Write("Enter latitude  [North = +  South = -]: ");
            string lat = Console.ReadLine();
            double latitude = Double.Parse(lat);

            Console.Write("Enter longitude [West = +  East = -]: ");
            string lng = Console.ReadLine();
            double longitude = Double.Parse(lng);

            test.Longitude = longitude;  // Examples: Tokyo, Japan (35.689487, -139.691706, offset -9)
            test.Latitude = latitude;    //           Marina, California (36.677660, 121.762049, offset +8)

            return test;
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
            Console.Write("\nMonth: ");
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

        static double[] julianToCalendar(double jDate)
        {            
            jDate += .5;
            int Z = (int)(Math.Floor(jDate));
            double F = jDate % 1;

            int a = 0;
            int A = Z;

            if (Z >= 2299161)
            {
                a = (int)((Z - 1867216.25) / 36524.25);
                A = Z + 1 + a - (int)(a / 4); 
            }

            int B = A + 1524;
            int C = (int)((B - 122.1) / 365.25);
            int D = (int)(365.25 * C);
            int E = (int)((B - D) / 30.6001);

            double day = B - D - (int)(30.6001 * E) + F;
            double month = E - 1;
            if (E >= 14)
            {
                month = E - 13;
            }

            double year = C - 4716;
            if (month <= 2)
            {
                year = C - 4715;
            }
            double hour = (day % 1) * 24;
            double minute = (hour % 1) * 60;
            int second = (int)((minute % 1) * 60);

            //Console.WriteLine("{0}   {1}   {2}   {3}   {4}   {5}", 
            //    month, Math.Floor(day), year, Math.Floor(hour), Math.Floor(minute), second);

            double[] calendar = {(month), (Math.Floor(day)), (year), (Math.Floor(hour)), (Math.Floor(minute)), (second)};
            return calendar;
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

        static void printSunriseSunset(double[] sunrise, double[] sunset)
        {
            Console.WriteLine("\nSunrise: {0}/{1}/{2} at {3}:{4}:{5}", sunrise[0], sunrise[1], sunrise[2], sunrise[3], sunrise[4], sunrise[5]);
            Console.WriteLine(  "Sunset:  {0}/{1}/{2} at {3}:{4}:{5}\n", sunset[0], sunset[1], sunset[2], sunset[3], sunset[4], sunset[5]);
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

        public class Location
        {

            public double Latitude
            {
                get;
                set;
            }

            public double Longitude
            {
                get;
                set;
            }

            public double[] Sunrise
            {
                get;
                set;
            }

            public double[] Sunset
            {
                get;
                set;
            }
            
            
        }
    }
} 