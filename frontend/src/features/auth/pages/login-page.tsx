import { LockKeyhole, Sparkles } from 'lucide-react';
import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { Navigate, useLocation, useNavigate } from 'react-router';

import { Button } from '@/components/ui/button';
import { FieldShell, Input } from '@/components/forms/field';
import { toAppError } from '@/lib/api-error';

import { useAuth } from '../hooks/use-auth';
import type { LoginInput } from '../api/auth-api';

export function LoginPage() {
  const { isAuthenticated, login, isSigningIn } = useAuth();
  const [formError, setFormError] = useState('');
  const navigate = useNavigate();
  const location = useLocation();
  const {
    register,
    handleSubmit,
    formState: { errors }
  } = useForm<LoginInput>({ defaultValues: { email: '', password: '', rememberMe: false } });

  if (isAuthenticated) return <Navigate to="/" replace />;

  const submit = handleSubmit(async (values) => {
    setFormError('');
    try {
      await login(values);
      const destination =
        typeof location.state === 'object' && location.state && 'from' in location.state
          ? String(location.state.from)
          : '/';
      navigate(destination, { replace: true });
    } catch (error) {
      setFormError(
        toAppError(error).status === 401 ? 'The email or password is incorrect.' : toAppError(error).message
      );
    }
  });

  return (
    <main className="login-page">
      <section className="login-showcase" aria-label="PeopleSpring introduction">
        <div className="brand brand--light">
          <span className="brand__mark">
            <Sparkles size={20} />
          </span>
          <span>PeopleSpring</span>
        </div>
        <div className="login-showcase__content">
          <span className="eyebrow eyebrow--light">Local-first people operations</span>
          <h1>Everything your people team needs, in one calm workspace.</h1>
          <p>
            Manage employee records, contracts, compensation, and documents with privacy-first local infrastructure.
          </p>
          <div className="login-proof">
            <div className="login-proof__avatars">
              <span>MS</span>
              <span>AK</span>
              <span>JL</span>
            </div>
            <p>
              <strong>Built for focused HR teams</strong>
              <br />
              Clear records. Thoughtful workflows. No clutter.
            </p>
          </div>
        </div>
        <p className="login-showcase__footer">Your HR data stays within your local environment.</p>
      </section>

      <section className="login-panel">
        <form className="login-form" onSubmit={submit} noValidate>
          <span className="login-form__icon">
            <LockKeyhole size={22} />
          </span>
          <div>
            <span className="eyebrow">Secure workspace</span>
            <h2>Welcome back</h2>
            <p>Sign in to continue to your HR workspace.</p>
          </div>
          {formError ? (
            <div className="form-alert" role="alert">
              {formError}
            </div>
          ) : null}
          <FieldShell label="Work email" htmlFor="email" required error={errors.email?.message}>
            <Input
              id="email"
              type="email"
              autoComplete="username"
              placeholder="name@company.com"
              {...register('email', {
                required: 'Email is required.',
                pattern: { value: /^\S+@\S+\.\S+$/, message: 'Enter a valid email address.' }
              })}
            />
          </FieldShell>
          <FieldShell label="Password" htmlFor="password" required error={errors.password?.message}>
            <Input
              id="password"
              type="password"
              autoComplete="current-password"
              placeholder="Enter your password"
              {...register('password', { required: 'Password is required.' })}
            />
          </FieldShell>
          <label className="checkbox-row">
            <input type="checkbox" {...register('rememberMe')} />
            <span>Keep me signed in on this device</span>
          </label>
          <Button type="submit" loading={isSigningIn}>
            Sign in to workspace
          </Button>
          <p className="login-form__help">Need access? Contact your HR administrator.</p>
        </form>
      </section>
    </main>
  );
}
