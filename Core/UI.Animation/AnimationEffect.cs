using System;
using System.Collections.Generic;
using Olive;

namespace Zebble
{
    public class AnimationEffect
    {
        public static void PushBounce(View view, float pushTo = .97f, float bounceTo = 1.05f)
        {
            var steps = new List<Animation>();

            steps.Add(Animation.Create(view, 100.Milliseconds(), b => b.Scale(pushTo)));

            steps.Add(Animation.Create(view, 100.Milliseconds(), b => b.Scale(bounceTo)));

            if (bounceTo != 1)
                steps.Add(Animation.Create(view, 200.Milliseconds(), b => b.Scale(1)));

            view.Animate(steps.ToArray()).GetAwaiter();
        }
    }
}