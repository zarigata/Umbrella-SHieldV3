# Dockerfile for SecureGuard KeyServer API
FROM python:3.10-slim
WORKDIR /app
COPY requirements.txt ./
RUN pip install --no-cache-dir -r requirements.txt
COPY app/ ./app
COPY db/schema.sql ./scripts
COPY scripts/ ./scripts
ENV FLASK_APP=app/app.py
EXPOSE 5000
CMD ["flask", "run", "--host=0.0.0.0", "--port=5000"]
