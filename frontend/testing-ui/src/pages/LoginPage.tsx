import { useState } from 'react';
import type { ApiResult } from '../api/client';
import { login } from '../api/services';
import { Field } from '../components/FormControls';
import { ResultPanel } from '../components/ResultPanel';
import type { AuthUser } from '../models';

export function LoginPage({ onLogin }: { onLogin: (user: AuthUser) => void }) {
  const [username, setUsername] = useState('admin');
  const [password, setPassword] = useState('Admin@12345');
  const [result, setResult] = useState<ApiResult<unknown> | null>(null);

  async function submit() {
    const response = await login(username, password);
    setResult(response);
    if (response.ok) onLogin(response.data);
  }

  return (
    <main className="login-page">
      <section className="panel login-panel">
        <h1>StockTrace Testing UI</h1>
        <p>Sign in with one of the testing users.</p>
        <Field label="Username" value={username} onChange={setUsername} />
        <Field label="Password" type="password" value={password} onChange={setPassword} />
        <button onClick={submit}>Login</button>
        <ResultPanel result={result} />
      </section>
    </main>
  );
}
