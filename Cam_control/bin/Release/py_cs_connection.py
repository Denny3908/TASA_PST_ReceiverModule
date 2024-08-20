import clr
from zaber_motion import Units
from zaber_motion.ascii import Connection
import serial

"""引入控制相機的C#函式庫"""
dll_path = r"C:/Users/9288/Desktop/Cam_control/Cam_control/bin/Release/Cam_control.dll" # 設定DLL的路徑
clr.AddReference(dll_path)
from Cam_control import *
cam = Cam_params() # type: ignore

"""拍攝流程：開燈 - 拍攝 - 關燈"""
def main():
    light = type('LightSource', (object,), {})()
    # light_on(light, 'COM4')
    capture_sequence()
    # cam.Connect()
    # cam.Scan()
    # light_off()
    
"""控制光源開啟"""
def light_on(light, port):
    light.port = port
    light.serial_port = serial.Serial(
        port = light.port,
        baudrate = 115200,
        parity = serial.PARITY_ODD,
        stopbits = serial.STOPBITS_ONE,
        bytesize = serial.EIGHTBITS,
        timeout = 3
    )
    light.icheck1 = True
    light.serial_port.write(f"{"level 1 4095"}\r\n".encode())  # 光源吃string指令
    light.serial_port.write(f"{"power 1 1"}\r\n".encode()) # 基本上只會用到level和power兩指令

"""控制光源關閉"""
def light_off(light):
    light.serial_port.write(f"{"power 1 0"}\r\n".encode())
    light.serial_port.close()

"""拍攝序列，包含Camera和Stage"""
def capture_sequence():
    # 連接到Stage
    connection = Connection.open_serial_port("COM3")
    connection.enable_alerts()
    device_list = connection.detect_devices()
    device = device_list[0]

    # 創建axis，測試移動並設回home
    stage = device.get_axis(1)
    stage.move_absolute(10000)
    stage.home()

    # 創建stage的encoder，會同時記錄理論上的pos和實際encode到的pos
    # encoder = device.oscilloscope
    # encoder.clear()
    # encoder.set_delay(0)
    # encoder.set_timebase(0.1, Units.TIME_MILLISECONDS) # 取樣最高頻率頻率10kHz

    # 從python傳遞變數給C#的config
    config_name = r"C:/Users/9288\Desktop/Cam_control/config.txt"

    # 這邊排程各組的拍攝參數，會導入cam的C#程式裡
    # 注意lineRate、exposureTime、gain三個list的elements數量要相同
    lineRate_seq = [1000, 1000, 1000, 600, 300, 300]
    exposureTime_seq = [4, 30, 200, 1500, 3000, 3000]
    gain_seq = [1, 1, 1, 1, 1, 10.0]
    
    # # 測試用快速參數
    # lineRate_seq = [1000]
    # exposureTime_seq = [200]
    # gain_seq = [1]
    
    # speed_seq = [5.7 * lineRate * 7.04 * (10**-3) for lineRate in lineRate_seq] # With len
    speed_seq = [2 * lineRate * 7.04 * (10**-3) for lineRate in lineRate_seq] # BIN2
    # print(speed_seq)
    
    # end_position_native = 607740
    # end_position_si = stage.settings.convert_from_native_units("pos", end_position_native, Units.LENGTH_MILLIMETRES)
    # print(end_position_si)

    
    for lineRate, exposureTime, gain, speed in zip(lineRate_seq, exposureTime_seq, gain_seq, speed_seq):
        with open(config_name, 'w') as config: # 寫一個config給cam_control讀
            config.write(f"{lineRate}\n{exposureTime}\n{gain}")
        
        # encoder.add_channel(1, 'pos')
        # encoder.add_channel(1, 'encoder.pos')

        capturing_done = False  # 添加此變數來追蹤是否已完成捕捉
        cam.Connect()

        if lineRate > 600:
            stage.home()
            stage.move_velocity(speed, Units.VELOCITY_MILLIMETRES_PER_SECOND)
            while True:
                position = stage.get_position(Units.LENGTH_MILLIMETRES)
                if 90 <= position and not capturing_done:
                    # encoder.start()
                    cam.Scan()
                    # print("Capturing")
                    capturing_done = True
                
                if position >= 300:
                    # encoder.stop()
                    stage.move_max()
                    stage.move_min()
                    stage.home()
                    break
                
        else:
            stage.home()
            stage.move_absolute(80, Units.LENGTH_MILLIMETRES)
            stage.move_velocity(speed, Units.VELOCITY_MILLIMETRES_PER_SECOND)
            while True:
                position = stage.get_position(Units.LENGTH_MILLIMETRES)
                if position >= 90 and not capturing_done:
                    # encoder.start()
                    cam.Scan()
                    # print("Capturing")
                    capturing_done = True
                
                if position >= 210:
                    # encoder.stop()
                    stage.move_max()
                    stage.move_min()
                    stage.home()
                    break


        # 每張拍完會輸出一個encoder紀錄的position檔案。主要是驗證用，整合後可關閉
        
        # stage_data = encoder.read()
        # stage_pos = stage_data[0]
        # stage_pos_sample = stage_pos.get_data(Units.LENGTH_MILLIMETRES)
        # stage_encode = stage_data[1]
        # stage_encode_sample = stage_encode.get_data(Units.LENGTH_MILLIMETRES)
        # stage_file_name = f"{exposureTime}us_Gain{gain}_stage_encode.csv"
        # with open(stage_file_name, 'w') as stage_file:
        #     stage_file.write("Time (ms), Command Position (mm), Encoded Position (mm)\n")
        #     for i in range(len(stage_pos_sample)):
        #         stage_file.write(f'{stage_pos.get_sample_time(i, Units.TIME_MILLISECONDS)}, ')
        #         stage_file.write(f'{stage_pos_sample[i]}, {stage_encode_sample[i]}\n')
        
        # encoder.clear()     

if __name__ == "__main__":
    main()