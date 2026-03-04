# ML_TrafficSimulator IN PROGRESS

ML_TrafficSimulator is a deterministic traffic simulation project built in Unity, connected to an AI-powered analytics server (YOLOv8). The project's goal is to study and analyze real-time traffic flow using Computer Vision methods.

Because of the specific, simplified vehicle graphics (low-poly/boxy cars), standard AI models struggle to identify them correctly out of the box. To solve this, the project includes a built-in pipeline to automatically collect data and fine-tune a custom object detection model.

 ![GithubVID](https://github.com/user-attachments/assets/a398b798-81ef-40af-90ae-ca4b2f9b041a)

## Key Features

* **Real-time Simulation:** A Unity 3D environment generating moving traffic (logical and visual).
* **REST API Backend:** A fast Python server built with `FastAPI` that receives image frames from the simulation.
* **AI Detection (YOLOv8):** Automatic vehicle counting on screenshots sent directly from the Unity camera.
* **Automated Dataset Collection:** A built-in server mechanism that randomly (10% chance) saves frames to a local folder. This makes it incredibly easy to gather a custom dataset to train YOLO on non-standard car models.

## Technologies & Architecture

* **Client / Simulation:** Unity 3D, C#
* **Server / Backend:** Python 3, FastAPI, Uvicorn
* **Artificial Intelligence:** Ultralytics YOLOv8, OpenCV, Pillow (PIL)

# Getting Started

The project consists of two main parts that must run simultaneously: the AI server and the Unity simulation.

## 1. Setting up the Python Server
**Personally I used both scripts on different anaconda environments.**

Ensure you have Python installed (3.9+ recommended). In your terminal, navigate to the main project folder and run the following commands:

### Install the required libraries
```python
pip install fastapi uvicorn ultralytics pillow

python main.py
```
or by using uvicorn directly:
```python
uvicorn main:app --host 0.0.0.0 --port 8000
```
## 2. Setting up and Starting the YOLO AI Server
The Python server acts as the brain of the operation, receiving image frames from Unity and processing them through the YOLOv8 AI model.

**Prerequisites:** Ensure you have Python 3.9+ installed. It is highly recommended to use a virtual environment to avoid dependency conflicts.

### Install the required libraries
pip install fastapi uvicorn ultralytics pillow

Running the Server:

Navigate to the directory containing your Python script (e.g., main.py) and start the FastAPI server:

```
python main.py
(Alternatively, you can run it directly via Uvicorn: uvicorn main:app --host 0.0.0.0 --port 8000)
```

Important Note: On the very first run, the ultralytics library will automatically download the pre-trained yolov8n.pt model weights. Once the download is complete, you should see a message in the console confirming that the YOLO AI Server is up and listening on http://localhost:8000 (or 0.0.0.0:8000).

## 3. Running the Unity Simulation
Open the project in the Unity Editor. The template scene is already made up.

Ensure the URL in your request-sending script (attached to your camera or manager) is set to http://127.0.0.1:8000/count_cars.

Hit the "Play" button. Unity will start sending frames to the server. Check your Python console to see real-time logs for vehicle counts and saved training images.

## License
This project is licensed under a Custom Non-Commercial License. 
You are free to download, study, and modify the code for personal and educational purposes, but **commercial use and selling are strictly prohibited**. See the `LICENSE` file for more details.
