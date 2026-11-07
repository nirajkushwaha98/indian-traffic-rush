using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.InputSystem;

namespace ITR
{
    /// <summary>3D backdrop for the menus: a turntable on a sunset highway with the selected car.</summary>
    public class MenuShowroom : MonoBehaviour
    {
        public Camera Cam { get; private set; }
        VehicleVisual car;
        Transform turntable;
        Transform ringGlow;
        float spin;
        float dragVel;
        float drop;
        bool garageView;
        float sirenT;

        // Portrait framing: home = car slightly above centre (between logo and buttons);
        // garage = car in the upper part of the screen, above the stats card.
        static readonly Vector3 HomePos = new Vector3(-2.2f, 2.6f, -9.2f);
        static readonly Vector3 HomeLook = new Vector3(-0.1f, 0.1f, 0f);
        static readonly Vector3 GaragePos = new Vector3(-2.0f, 2.2f, -8.4f);
        static readonly Vector3 GarageLook = new Vector3(-0.1f, -2.2f, 0f);
        const float HomeHFov = 46f, GarageHFov = 44f;
        float lastAspect = -1f;

        public static MenuShowroom Create()
        {
            var go = new GameObject("Showroom");
            var s = go.AddComponent<MenuShowroom>();
            s.Build();
            return s;
        }

        void Build()
        {
            var sun = EnvironmentTheme.CreateSun();
            var road = new GameObject("Road").AddComponent<RoadManager>();
            road.Init(8, 1234, -100f);
            EnvironmentTheme.Apply(TimeOfDay.Sunset, sun, 40f, 260f);

            turntable = new GameObject("Turntable").transform;
            turntable.position = new Vector3(0f, 0f, 0f);
            var a = new MeshAssembler();
            a.Cylinder(MaterialFactory.Lit(new Color(0.1f, 0.1f, 0.12f), 0.8f), new Vector3(0, 0.08f, 0), 6.2f, 0.16f, Vector3.zero);
            a.Cylinder(MaterialFactory.Lit(new Color(0.2f, 0.2f, 0.23f), 0.6f), new Vector3(0, 0.17f, 0), 5.6f, 0.04f, Vector3.zero);
            a.Build("Platform", turntable, false);
            var ga = new MeshAssembler();
            ga.Cylinder(MaterialFactory.Unlit(UIKit.Saffron), new Vector3(0, 0.12f, 0), 6.35f, 0.1f, Vector3.zero);
            ringGlow = ga.Build("Ring", null, false).transform;
            ringGlow.position = Vector3.zero;

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            Cam = camGo.AddComponent<Camera>();
            Cam.fieldOfView = CameraRig.VerticalFov(HomeHFov, Cam.aspect);
            Cam.nearClipPlane = 0.2f;
            Cam.farClipPlane = 320f;
            camGo.AddComponent<AudioListener>();
            var data = camGo.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = false;
            camGo.transform.position = HomePos;
            camGo.transform.LookAt(HomeLook);

            // Warm key light on the car
            var key = new GameObject("KeyLight").AddComponent<Light>();
            key.type = LightType.Point;
            key.range = 14f;
            key.intensity = 6f;
            key.color = new Color(1f, 0.9f, 0.8f);
            key.transform.position = new Vector3(-3f, 4f, -3f);
        }

        public void ShowCar(VehicleData v, Color paint)
        {
            if (car != null) Destroy(car.gameObject);
            var go = VehicleBuilder.Build(v.shape, paint, true);
            go.transform.SetParent(turntable, false);
            go.transform.localPosition = new Vector3(0, 0.19f, 0);
            car = go.GetComponent<VehicleVisual>();
            car.SetHeadlights(true);
            drop = 1f;
        }

        public void SetGarageView(bool on) => garageView = on;

        void Update()
        {
            float dt = Time.deltaTime;

            // Drag to rotate (mouse or one finger), otherwise idle spin.
            float dragX = 0f;
            var m = Mouse.current;
            var es = UnityEngine.EventSystems.EventSystem.current;
            bool overUI = es != null && es.IsPointerOverGameObject();
            if (m != null && m.leftButton.isPressed && !overUI)
                dragX = m.delta.ReadValue().x;
            var ts = Touchscreen.current;
            if (ts != null && ts.primaryTouch.press.isPressed)
            {
                var pos = ts.primaryTouch.position.ReadValue();
                // Portrait: drag on the middle band of the screen (where the car is) to rotate it.
                if (pos.y > Screen.height * 0.35f && pos.y < Screen.height * 0.85f) dragX = ts.primaryTouch.delta.ReadValue().x;
            }
            if (Mathf.Abs(dragX) > 0.01f) dragVel = -dragX * 12f;
            dragVel = Mathf.Lerp(dragVel, 0f, dt * 3f);
            spin += (18f + dragVel) * dt;
            turntable.rotation = Quaternion.Euler(0, spin, 0);

            if (car != null)
            {
                drop = Mathf.MoveTowards(drop, 0f, dt * 3f);
                float y = 0.19f + drop * drop * 1.5f;
                car.transform.localPosition = new Vector3(0, y, 0);
                car.SpinWheels(0f, dt);
                if (car.sirenRed != null)
                {
                    sirenT += dt;
                    bool ph = (sirenT % 0.5f) < 0.25f;
                    car.sirenRed.enabled = ph;
                    car.sirenBlue.enabled = !ph;
                }
            }

            var pos3 = garageView ? GaragePos : HomePos;
            var look = garageView ? GarageLook : HomeLook;
            float vfov = CameraRig.VerticalFov(garageView ? GarageHFov : HomeHFov, Cam.aspect);
            if (Mathf.Abs(Cam.aspect - lastAspect) > 0.01f) { Cam.fieldOfView = vfov; lastAspect = Cam.aspect; }
            else Cam.fieldOfView = Mathf.Lerp(Cam.fieldOfView, vfov, 1f - Mathf.Exp(-6f * dt));
            var t = Cam.transform;
            t.position = Util.Damp(t.position, pos3, 4f, dt);
            var targetRot = Quaternion.LookRotation(look - t.position);
            t.rotation = Quaternion.Slerp(t.rotation, targetRot, 1f - Mathf.Exp(-6f * dt));
        }
    }
}
