
using Microsoft.Kinect;
using System;
using System.IO;
using static System.Net.Mime.MediaTypeNames;
using System.Globalization;
using System.Windows.Forms;
using System.Security.Cryptography.X509Certificates;
using System.Net;

public class KinectInputController 
{

    public SkeletonStream Start(KinectSensor kinectSensor)
    {
        kinectSensor.Start();
        Console.WriteLine("Kinnect processes have started!");
        //kinectSensor.ElevationAngle = 14;

        return StartStream(kinectSensor);
    }

    public SkeletonStream StartStream(KinectSensor kinectSensor)
    {
        SkeletonStream stream = kinectSensor.SkeletonStream;
        stream.Enable();

        stream.EnableTrackingInNearRange= true;

        stream.TrackingMode = SkeletonTrackingMode.Default;

        return stream;
    }

    public Boolean Loop(KinectSensor kinectSensor, SkeletonStream stream)
    {
        int i = 0;
        int old_best_trackable_pt_x = 0;
        int old_best_trackable_pt_y = 0;
        int old_best_trackable_pt_z = 0;

        int old_act = 0;
        int lastTrackedVal = -1;
        while (i < 1000000)
        {

            SkeletonFrame frame = stream.OpenNextFrame(0);
            if (frame != null)
            {
                Skeleton[] skeletonArr = new Skeleton[frame.SkeletonArrayLength];
                frame.CopySkeletonDataTo(skeletonArr);

                if (skeletonArr.Length > 0 && skeletonArr[0] != null)
                {
                    Skeleton trackedSkeleton = skeletonArr[0];
                    JointCollection trackedSkeletonJoints = trackedSkeleton.Joints;

                    JointType jointType = JointType.Head; // Replace with desired joint type
                    Joint bestTrackableJoint = trackedSkeletonJoints[jointType];

                    Position coords = new Position(0, 0, 3);
                    Jointy darkestTrackablePoint = new Jointy(coords);

                    //Step 3: Trig
                    //p: darkest point, k: kinect point(origin), j: person (head) point, Sides are across from corresponding angles
                    double dotOfPandJ = bestTrackableJoint.Position.X * darkestTrackablePoint.Position.X +
                            bestTrackableJoint.Position.Z * darkestTrackablePoint.Position.Z;

                    double J = Math.Sqrt(Math.Pow(bestTrackableJoint.Position.X, 2) +
                        Math.Pow(bestTrackableJoint.Position.Z, 2));

                    double P = Math.Sqrt(Math.Pow(darkestTrackablePoint.Position.X, 2) +
                        Math.Pow(darkestTrackablePoint.Position.Z, 2));

                    double k = (Math.PI / 2) - (Math.Asin((dotOfPandJ) / (J * P)));

                    double K = Math.Sqrt(Math.Pow(J, 2) * Math.Pow(P, 2) - 2 * J * P * Math.Cos(k));

                    double p = Math.Asin((Math.Sin(k) * K) / P) * (180 / Math.PI);

                    p = bestTrackableJoint.Position.X > 0 ? -p : p;

                    if (i % 100 == 0)
                    {
                        Console.WriteLine("[Frame " + i + "] Coords: X: " + bestTrackableJoint.Position.X + " Y: " + bestTrackableJoint.Position.Y + " Z: " + bestTrackableJoint.Position.Z);
                    }
                    if (bestTrackableJoint.Position.X != 0 || bestTrackableJoint.Position.Y != 0 || bestTrackableJoint.Position.Z != 0)
                    {
                        lastTrackedVal = i;
                        //Console.WriteLine("[Frame " + i + "] Coords: X: " + bestTrackableJoint.Position.X + " Y: " + bestTrackableJoint.Position.Y + " Z: " + bestTrackableJoint.Position.Z);
                        //Console.WriteLine("Angle: " + p + "°\n");
                        //Console.WriteLine("Debugging: J=" + J + ", P=" + P + ", k=" + k * (180 / Math.PI) + ", K=" + K);

                        p *= 3;
                        int act = (int)p + 188;

                        //Console.WriteLine("ACT: " + p);

                        if (act < 110)
                        {
                            act = 110;
                        }
                        else if (act > 255)
                            act = 255;

                        //Console.WriteLine($"ACT: {act}");
                        //Console.WriteLine($"OLD-ACT: {old_act}");

                        if (Math.Abs(old_act - act) > 0)
                        {
                            //Console.WriteLine($"writiing to https://www.xcribbage.com/api/v0/push?s={act}");
                            Send(act);
                            Console.WriteLine("[Frame " + i + "] Coords: X: " + bestTrackableJoint.Position.X + " Y: " + bestTrackableJoint.Position.Y + " Z: " + bestTrackableJoint.Position.Z);
                            old_act = act;
                        }
                    }
                    else if(i - lastTrackedVal > 40)
                    {
                        EndStream(kinectSensor);
                        //Console.WriteLine("OFF");
                        StartStream(kinectSensor);
                        //Console.WriteLine("ON");
                        lastTrackedVal = i;
                    }
                    //int actuator_val = 
                    //Step 4: Calculate linear actuator position
                    //Step 5: Send results to server
                    //Step 6: Disposes of the skeleton frame
                    /*old_best_trackable_pt_x = (int)bestTrackableJoint.Position.X;
                    old_best_trackable_pt_y = (int)bestTrackableJoint.Position.Y;
                    old_best_trackable_pt_z = (int)bestTrackableJoint.Position.Z;*/
                    i++;
                }
                frame.Dispose();
            }
        }
        return true;

    }

    public String Send(int act)
    {
        string url = "https://www.xcribbage.com/api/v0/push?s=" + act;
        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
        request.Method = "GET"; // or "POST" if the API expects a POST request

        try
        {
            // Get the response
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                // Read the response (optional, if needed)
                using (var responseStream = response.GetResponseStream())
                {
                    if (responseStream != null)
                    {
                        using (var reader = new System.IO.StreamReader(responseStream))
                        {
                            string responseText = reader.ReadToEnd();
                            //Console.WriteLine("Response received: " + responseText);
                            return responseText;
                        }
                    }
                }
            }
        }
        catch (WebException ex)
        {
            // Handle exceptions
            Console.WriteLine("Error: " + ex.Message);
            return "ERROR";
        }
        return "";
    }

    public Boolean EndStream(KinectSensor kinectSensor)
    {
        SkeletonStream stream = kinectSensor.SkeletonStream;
        stream.Disable();

        stream.EnableTrackingInNearRange = true;

        stream.AppChoosesSkeletons = false; //Change later if you want to change how skeletons are tracked

        return true;
    }
    public void Stop(KinectSensor kinectSensor)
    {

        EndStream(kinectSensor);

        //kinectSensor.ElevationAngle = kinectSensor.MaxElevationAngle;

        kinectSensor.Stop();

        Console.WriteLine("Kinnect processes have stopped!");

    }


}

class MainClass
{
    static void Main(string[] args)
    {
        KinectInputController controller = new KinectInputController();
        

        // Find a Kinect sensor

        KinectSensorCollection kinectSensors = KinectSensor.KinectSensors;

        KinectSensor kinectSensor = kinectSensors[0];


        SkeletonStream stream = controller.Start(kinectSensor);
        controller.Loop(kinectSensor, stream);
        controller.Stop(kinectSensor);


        // Wait for input to keep the console window open
        Console.ReadKey();
    }




    public void TakePicture(KinectSensor kinectSensor)
    {

        ColorImageStream stream = kinectSensor.ColorStream;
        stream.Enable();


        ColorImageFrame frame = stream.OpenNextFrame(1000);

        if (frame != null)
        {
            try
            {
                int width = frame.Width;
                int height = frame.Height;

                byte[] pixelData = new byte[frame.PixelDataLength];
                frame.CopyPixelDataTo(pixelData);



                string time = System.DateTime.Now.ToString("hh'-'mm'-'ss", CultureInfo.CurrentUICulture.DateTimeFormat);

                string myPhotos = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

                string path = Path.Combine(myPhotos, "KinectSnapshot-" + time + ".png");

                //        MyPic() { };

                using (FileStream fs = new FileStream(path, FileMode.Create))
                {
                    //             encoder.Save(fs);
                }


                /*MemoryStream ms = new MemoryStream();
                ms.Write(pixelData, 0, pixelData.Length);
                System.Drawing.Image returnImage = System.Drawing.Image.FromStream(ms);*/
            }
            catch
            {
                Console.WriteLine("There was an error with saving the image");
            }
            finally
            {
                frame.Dispose();
            }
        }
        else
        {

            // Handle the case where no frame was available within the timeout period.
            Console.WriteLine("No color frame available within the timeout period.");

        }

    }

    private static void SaveImage(ColorImageFrame data, Stream saveStream)
    {
        //             var encoder = BitmapEncoder.Create(data.Decoder.CodecInfo.ContainerFormat);
        //            encoder.Frames.Add(data);

        using (var memoryStream = new MemoryStream())
        {
            //          encoder.Save(memoryStream);
            memoryStream.Position = 0;
            memoryStream.CopyTo(saveStream);
        }
    }
}

public struct Position
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Z { get; set; }

    public Position(int x, int y, int z)
    {
        this.X = x;
        this.Y = y;
        this.Z = z;
    }

}

public struct Jointy
{
    public Position Position;
    public Jointy(Position coords)
    {
        this.Position = coords;
    }
}