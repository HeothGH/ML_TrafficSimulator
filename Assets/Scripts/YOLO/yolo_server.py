from fastapi import FastAPI, UploadFile, File
import uvicorn
from ultralytics import YOLO
from PIL import Image
import io
import random
import os
import time

DATASET_FOLDER = "dataset_unity"
if not os.path.exists(DATASET_FOLDER):
    os.makedirs(DATASET_FOLDER)
    print(f"[System] Utworzono folder na dane treningowe: {DATASET_FOLDER}")

app = FastAPI()

print("[AI] Ładowanie modelu YOLOv8...")
model = YOLO('yolov8n.pt')

VEHICLE_CLASSES = [2, 3, 5, 7]


@app.post("/count_cars")
async def count_cars(file: UploadFile = File(...)):
    try:
        image_bytes = await file.read()

        image = Image.open(io.BytesIO(image_bytes))

        if random.randint(1, 10) == 1:
            # Czas w milisekundach, aby nazwy plików się nie powtarzały
            timestamp = int(time.time() * 1000)
            file_path = os.path.join(DATASET_FOLDER, f"unity_frame_{timestamp}.jpg")


            image.convert("RGB").save(file_path, format="JPEG")
            print(f"[Dataset] 📸 Zapisano nową klatkę do treningu: {file_path}")

        results = model(image, conf=0.30, verbose=False)

        vehicle_count = 0

        for box in results[0].boxes:
            class_id = int(box.cls[0])
            if class_id in VEHICLE_CLASSES:
                vehicle_count += 1

        return {"status": "success", "cars": min(vehicle_count, 8)}

    except Exception as e:
        print(f"[Error] Coś poszło nie tak: {e}")
        return {"status": "error", "cars": 0, "message": str(e)}


if __name__ == "__main__":
    print("[Server] Startuję serwer analityczny na http://localhost:8000 ...")
    uvicorn.run(app, host="0.0.0.0", port=8000)