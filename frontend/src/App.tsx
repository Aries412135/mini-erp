import { useEffect, useState } from "react";
import axiosClient from "./api/axiosClient";

interface HealthResponse {
  status: string;
  time: string;
}

function App() {
  const [health, setHealth] = useState<HealthResponse | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    axiosClient
      .get<HealthResponse>('/api/health')
      .then((res) => setHealth(res.data))
      .catch((err) => setError(err.message));
  }, []);

  if (error) return <p>Error: {error}</p>;
  if (!health) return <p>Loading...</p>;

  return (
    <div>
      <h1>Mini ERP</h1>
      <p>Backend status: <strong>{health.status}!</strong></p>
    </div>
  )
}

export default App;