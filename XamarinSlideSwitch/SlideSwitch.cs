using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Graphics;
using Android.Util;
using Android.Content.Res;
using System.Threading.Tasks;
using System.Threading;

namespace XamarinSlideSwitch
{
    public class SlideSwitch : View
    {
        public const int SHAPE_RECT = 1;
        public const int SHAPE_CIRCLE = 2;
        private const int RIM_SIZE = 6;
        private static Color COLOR_THEME = Color.ParseColor("#ff00ee00");

        private Color color_theme;
        private bool isOpen;
        private int shape;

        private Paint paint;
        private Rect backRect;
        private Rect frontRect;
        private int alpha;
        private int max_left;
        private int min_left;
        private int frontRect_left;
        private int frontRect_left_begin = RIM_SIZE;
        private int eventStartX;
        private int eventLastX;
        private int diffX = 0;
        private bool slideable = true;
        private ISlideListener listener;

        public SlideSwitch(Context context, IAttributeSet attrs, int defStyleAttr)
            : base(context, attrs, defStyleAttr)
        {
            listener = null;
            paint = new Paint();
            paint.AntiAlias = true;
            
            TypedArray a = context.ObtainStyledAttributes(attrs, Resource.Styleable.slideswitch);
            color_theme = a.GetColor(Resource.Styleable.slideswitch_themeColor, COLOR_THEME);
            isOpen = a.GetBoolean(Resource.Styleable.slideswitch_isOpen, false);
            shape = a.GetInt(Resource.Styleable.slideswitch_shape, SHAPE_RECT);
            a.Recycle();
        }

        public SlideSwitch(Context context, IAttributeSet attrs)
            : this(context, attrs, 0) { }

        public SlideSwitch(Context context)
            : this(context, null) { }

        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
        {
            base.OnMeasure(widthMeasureSpec, heightMeasureSpec);
            int width = MeasureDimension(280, widthMeasureSpec);
            int height = MeasureDimension(140, heightMeasureSpec);
            if (shape == SHAPE_CIRCLE)
            {
                if (width < height)
                {
                    width = height * 2;
                }
            }
            SetMeasuredDimension(width, height);
            InitDrawingVal();
        }

        public void InitDrawingVal()
        {
            int width = MeasuredWidth;
            int height = MeasuredHeight;

            backRect = new Rect(0, 0, width, height);
            min_left = RIM_SIZE;
            if (shape == SHAPE_RECT)
            {
                max_left = width / 2;
            }
            else
            {
                max_left = width - (Height - 2 * RIM_SIZE) - RIM_SIZE;
            }

            if (isOpen)
            {
                frontRect_left = max_left;
                alpha = 255;
            }
            else
            {
                frontRect_left = RIM_SIZE;
                alpha = 0;
            }
            frontRect_left_begin = frontRect_left;
        }

        public int MeasureDimension(int p, int widthMeasureSpec)
        {
            int result = 0;
            var specMode = MeasureSpec.GetMode(widthMeasureSpec);
            int specSize = MeasureSpec.GetSize(widthMeasureSpec);
            if (specMode == MeasureSpecMode.Exactly)
            {
                result = specSize;
            }
            else
            {
                result = p;
                if (specMode == MeasureSpecMode.AtMost)
                {
                    result = Math.Min(result, specSize);
                }
            }
            return result;
        }

        protected override void OnDraw(Canvas canvas)
        {
            if (shape == SHAPE_RECT)
            {
                paint.Color = Color.Gray;
                canvas.DrawRect(backRect, paint);
                paint.Color = color_theme;
                paint.Alpha = alpha;
                canvas.DrawRect(backRect, paint);

                frontRect = new Rect(frontRect_left, RIM_SIZE, frontRect_left + MeasuredWidth / 2 - RIM_SIZE,
                    MeasuredHeight - RIM_SIZE);
                paint.Color = Color.White;
                canvas.DrawRect(frontRect, paint);
            }
            else
            {
                int radius;
                radius = backRect.Height() / 2 - RIM_SIZE;
                paint.Color = Color.Gray;
                canvas.DrawRoundRect(new RectF(backRect), radius, radius, paint);
                paint.Color = color_theme;
                paint.Alpha = alpha;
                canvas.DrawRoundRect(new RectF(backRect), radius, radius, paint);

                frontRect = new Rect(frontRect_left, RIM_SIZE, frontRect_left
                    + backRect.Height() - 2 * RIM_SIZE, backRect.Height() - RIM_SIZE);
                paint.Color = Color.White;
                canvas.DrawRoundRect(new RectF(frontRect), radius, radius, paint);
            }
        }

        public override bool OnTouchEvent(MotionEvent e)
        {
            if (slideable == false)
                return base.OnTouchEvent(e);
            switch (e.Action)
            {
                case MotionEventActions.Down:
                    {
                        eventStartX = (int)e.RawX;
                    }
                    break;
                case MotionEventActions.Move:
                    {
                        eventLastX = (int)e.RawX;
                        diffX = eventLastX - eventStartX;
                        int tempX = diffX + frontRect_left_begin;
                        tempX = (tempX > max_left ? max_left : tempX);
                        tempX = (tempX < min_left ? min_left : tempX);
                        if (tempX >= min_left && tempX <= max_left)
                        {
                            frontRect_left = tempX;
                            alpha = (int)(255 * (float)tempX / (float)max_left);
                            InvalidateView();
                        }
                    }
                    break;
                case MotionEventActions.Up:
                    {
                        int wholeX = (int)(e.RawX - eventStartX);
                        frontRect_left_begin = frontRect_left;
                        bool toRight;
                        toRight = (frontRect_left_begin > max_left / 2 ? true : false);
                        if (Math.Abs(wholeX) < 3)
                        {
                            toRight = !toRight;
                        }
                        MoveToDest(toRight);
                    }
                    break;
                default:
                    break;
            }
            return true;
        }

        private void MoveToDest(bool toRight)
        {
            Handler handler = new Handler(x =>
            {
                if (x.What == 1)
                {
                    listener.Open();
                }
                else
                {
                    listener.Close();
                }
            });

            ThreadPool.QueueUserWorkItem(x =>
            {
                if (toRight)
                {
                    while (frontRect_left <= max_left)
                    {
                        alpha = (int)(255 * (float)frontRect_left / (float)max_left);
                        InvalidateView();
                        frontRect_left += 3;
                        try
                        {
                            Thread.Sleep(3);
                        }
                        catch (Exception e) { }
                    }
                    alpha = 255;
                    frontRect_left = max_left;
                    isOpen = true;
                    if (listener != null)
                        handler.SendEmptyMessage(1);
                    frontRect_left_begin = max_left;
                }
                else
                {
                    while (frontRect_left >= min_left)
                    {
                        alpha = (int)(255 * (float)frontRect_left / (float)max_left);
                        InvalidateView();
                        frontRect_left -= 3;
                        try
                        {
                            Thread.Sleep(3);
                        }
                        catch (Exception e) { }
                    }
                    alpha = 0;
                    frontRect_left = min_left;
                    isOpen = false;
                    if (listener != null)
                        handler.SendEmptyMessage(0);
                    frontRect_left_begin = min_left;
                }
            });
        }

        public void InvalidateView()
        {
            if (Looper.MainLooper == Looper.MyLooper())
            {
                Invalidate();
            }
            else
            {
                PostInvalidate();
            }
        }

        public void SetSlideListener(ISlideListener listener)
        {
            this.listener = listener;
        }

        public void SetState(bool isopen)
        {
            isOpen = isopen;
            InitDrawingVal();
            InvalidateView();
            if (listener != null)
            {
                if (isopen == true)
                {
                    listener.Open();
                }
                else
                {
                    listener.Close();
                }
            }
        }

        public void SetShapeType(int shapeType)
        {
            this.shape = shapeType;
        }

        public void SetSlideable(bool slideable)
        {
            this.slideable = slideable;
        }

        protected override void OnRestoreInstanceState(IParcelable state)
        {
            if (state is Bundle)
            {
                Bundle bundle = (Bundle)state;
                this.isOpen = bundle.GetBoolean("isOpen");
                state = (IParcelable)bundle.GetParcelable("instanceState");
            }
            base.OnRestoreInstanceState(state);
        }

        protected override IParcelable OnSaveInstanceState()
        {
            Bundle bundle = new Bundle();
            bundle.PutParcelable("instanceState", base.OnSaveInstanceState());
            bundle.PutBoolean("isOpen", this.isOpen);
            return base.OnSaveInstanceState();
        }
    }
}