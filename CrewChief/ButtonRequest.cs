using CrewChief.Events;
using SharpDX.DirectInput;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace CrewChief
{
    /**
     * bollocks test class while I work out how to get button events from SharpDX without a 'forms' 
     * application window
     */
    class ButtonRequest : Request
    {
        DirectInput directInput = new DirectInput();
        Boolean listening = true;
        Joystick joystickToUse;
        int buttonIndex = -1;

        public Boolean isAssignedButtonPressed()
        {
            if (joystickToUse == null || buttonIndex == -1)
            {
                return false;
            }
            JoystickState state = joystickToUse.GetCurrentState();
            return state.Buttons[buttonIndex];
        }

        public void getFirstPressedButton() {
            while (listening)
            {
                foreach (var deviceInstance in directInput.GetDevices(DeviceType.Driving,
                DeviceEnumerationFlags.AllDevices))
                {
                    Guid joystickGuid = deviceInstance.InstanceGuid;
                    if (joystickGuid == Guid.Empty)
                    {
                        listening = false;
                    }
                    else
                    {
                        // Instantiate the joystick
                        var joystick = new Joystick(directInput, joystickGuid);

                        Console.WriteLine("Found Joystick/Gamepad with GUID: {0}", joystickGuid);
                        
                        // Acquire the joystick
                        joystick.Acquire();
                        JoystickState state = joystick.GetCurrentState();
                        Boolean[] buttons = state.Buttons;
                        Boolean useThisJoystick = false;
                        for (int i = 0; i < buttons.Count(); i++)
                        {
                            if (buttons[i])
                            {
                                this.joystickToUse = joystick;
                                this.buttonIndex = i;
                                Console.WriteLine("Using button index " + buttonIndex + " for device guid " + joystickGuid);
                                listening = false;
                                useThisJoystick = true;
                                break;
                            }
                        }
                        if (!useThisJoystick)
                        {
                            joystick.Unacquire();
                        }
                    }
                }
                Thread.Sleep(1000);
            }
            Console.WriteLine("Got button " + buttonIndex);
        }

        public void requestResponse(String responseClassName)
        {
            AbstractEvent abstractEvent = CrewChief.getEvent(responseClassName);
            abstractEvent.respond();
        }
    }
}
