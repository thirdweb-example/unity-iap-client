// WARNING: Do not modify! Generated file.

namespace UnityEngine.Purchasing.Security {
    public class GooglePlayTangle
    {
        private static byte[] data = System.Convert.FromBase64String("pX1Vvh0MzWKE/0MUChX8+hrDZfsaATxJmKeeYfPmJFjrH47Q/iRRc99t7s3f4unmxWmnaRji7u7u6u/sXTic40P9cwsVu4OnJvUrsSu7xYpyQ1AgOKlW/hyu0Yq9dtxOQM0lQu4NwUk2C/7llMNsAXS57SIjSOHjbe7g799t7uXtbe7u70uZO18u2f/NxRXB537F4kCvBYyFawj2LBOXpH4YlqfFzgt4TMkJYK5OT2tLqtWqH+rdKL0dMMPLoh7R/G0cHwmul54VN9OKW1tGNsmqAvd5iEBwP8rKY89o2JHbwOjFv17wJFdvK+2MGZcjEhmiJBkjpTK+OqMGcyyJgTZMRmjHEN14unAvNz9mX5+xdLAKRm9662BMwaoCLUc7Wu3s7u/u");
        private static int[] order = new int[] { 5,11,5,8,12,8,11,7,12,11,13,13,12,13,14 };
        private static int key = 239;

        public static readonly bool IsPopulated = true;

        public static byte[] Data() {
        	if (IsPopulated == false)
        		return null;
            return Obfuscator.DeObfuscate(data, order, key);
        }
    }
}
