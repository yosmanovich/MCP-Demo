FROM python:3.11-slim-bullseye
WORKDIR /usr/src/app
COPY requirements.txt .
RUN apt-get update && apt-get upgrade -y && apt-get install -y --no-install-recommends gcc && \
	pip install --no-cache-dir --upgrade pip && \
	pip install --no-cache-dir --upgrade -r requirements.txt && \
	apt-get remove -y gcc && apt-get autoremove -y && apt-get clean && rm -rf /var/lib/apt/lists/*
COPY . .
CMD ["python", "./main.py"]