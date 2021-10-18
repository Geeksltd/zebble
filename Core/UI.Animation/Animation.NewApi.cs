// using System;
// using System.Collections.Generic;
// using System.Text;

// namespace Zebble
// {
//  partial  class Animation
//    {
//        public enum VisualFeature { Opacity, ScaleX, ScaleY }

//        public class Effect
//        { 
//            public View Target;
//            public object From, To;
//            public VisualFeature Feature;

//            public TimeSpan? Duration { get; set; } 
//            public TimeSpan? Delay { get; set; }             
//            public AnimationEasing? Easing { get; set; }             
//            public EasingFactor? EasingFactor { get; set; } 

//            internal void SetFromValue(object value) { 
//            if (value == null)
//                {
//                    // TODO: Read from the target
//                }
//            }

//            public Effect(  View target, VisualFeature feature, object finalValue, object startValue, TimeSpan? delay)
//            {
//                Feature = feature;
//                Target = target;
//                From = startValue ?? ReadExistingValue();
//                // ...

//                Delay = delay  ;
//            }

//            object ReadExistingValue()
//            {
//                switch (Feature)
//                {
//                    case VisualFeature.Opacity: return Target.Opacity;
//                }
//            }
//        }

//        public readonly List<Effect> Effects = new List<Effect>();

//        public Animation  Add(View target, VisualFeature feature, object finalValue, object startValue = null, TimeSpan? durationIfDifferent = null, TimeSpan? delay = null)
//        {
//            Effects.Add(new Effect(target, feature, finalValue, startValue, ...));

//            return this;
//        }

//    }
// }