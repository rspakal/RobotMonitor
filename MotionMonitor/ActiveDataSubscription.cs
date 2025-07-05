using MotionMonitor.Enums;
using System;
using System.Net.Http.Headers;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.Intrinsics.X86;

namespace MotionMonitor
{
    public class ActiveDataSubscription : DataSubscription
    {
        private readonly LogDataValueFormat _format;
        private readonly int _blockSize;
        private bool _enabled;

        public LogDataValueFormat Format => _format;
        public int BlockSize => _blockSize;
        public bool Enabled => _enabled;

        public ActiveDataSubscription(
            int channelNo, 
            int signalNo, 
            string mechUnitName, 
            int axisNo, 
            float sampleTime, 
            LogDataValueFormat format,
            int blockSize,
            bool enabled) :
            base(channelNo, signalNo, mechUnitName, axisNo, sampleTime)
        {
            _format = format;
            _blockSize = blockSize;
            _enabled = enabled;
        }

        public static ActiveDataSubscription BuildDataSubscription(ReadDataBuffer buffer)
        {
            var channelNo = buffer.ReadInt();
            var signalNo = buffer.ReadInt();
            var mechUnitName = buffer.ReadString(MECH_UNIT_NAME_LENGTH);
            var axisNo = buffer.ReadInt();
            var format = (LogDataValueFormat)buffer.ReadInt();
            var sampleTime = buffer.ReadFloat();
            var blockSize = buffer.ReadInt();
            var enabled = signalNo != 0;

            return new ActiveDataSubscription(channelNo, signalNo, mechUnitName, axisNo, sampleTime, format, blockSize, enabled);
        }


        //public static List<DataSubscription> BuildRequestedDataSubscribtions()
        //{
        //    List<DataSubscription> subscriptions = new();
        //    int signalNo;
        //    int axisNo = 1;
        //    for (int i = 0; i < 12; i++)
        //    {
        //        signalNo = i <= 6 ? VELOCITY_SIGNAL : TORQUE_SIGNAL;
        //        if (i < 6)
        //        {
        //            axisNo = i + 1;
        //            signalNo = VELOCITY_SIGNAL;
        //        }
        //        else
        //        {
        //            axisNo = i - 5;
        //            signalNo = TORQUE_SIGNAL;
        //        }

        //        subscriptions.Add(new DataSubscription(i, signalNo, ROBOT_NAME, axisNo, AXC_SAMPLE_TIME));
        //    }
        //    return subscriptions;
        //}

    }


}
