from fastapi import FastAPI

app = FastAPI(
  title="ASTREE Claims AI Service",
  version="1.0.0",
)

@app.get("/health")
def health ():
  return {
    "service": "astree-ai-service",
    "status": "healthy",
  }
  