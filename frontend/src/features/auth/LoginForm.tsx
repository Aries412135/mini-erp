import { useForm } from "react-hook-form";
import { loginSchema, type LoginFormValues } from "./loginSchema";
import { zodResolver } from "@hookform/resolvers/zod";
import { useLogin } from "./useLogin";
import { getErrorMessage } from "../../utils/getErrorMessage";

const labelClass = 'mt-4 text-sm font-semibold text-slate-700';
const inputClass = 
    'rounded-lg border border-slate-300 px-3 py-2 text-base outline-none ' +
    'focus:border-blue-600 focus:ring-2 focus:ring-blue-200 ' +
    'aria-[invalid=true]:border-red-600';
const fieldErrorClass = 'text-sm text-red-700';

export function LoginForm() {
    const {
        register,
        handleSubmit,
        formState: { errors },
    } = useForm<LoginFormValues>({
        resolver: zodResolver(loginSchema),
        defaultValues: { email: '', password: '' },
    });

    const login = useLogin();

    return (
        <form 
            className="flex w-full max-w-sm flex-col gap-1 rounded-xl border border-slate-200 bg-white p-8 shadow-sm"
            noValidate
            onSubmit={handleSubmit((values) => login.mutate(values))}
        >
            <h1 className="text-2xl font-bold text-slate-900">Mini ERP</h1>
            <p className="mb-4 text-slate-500">Masuk untuk melanjutkan</p>

            {login.isError && (
                <div className="rounded-lg bg-red-50 px-3 py-2.5 text-sm text-red-700" role="alert">
                    {getErrorMessage(login.error)}
                </div>
            )}

            <label htmlFor="email" className={labelClass}>Email</label>
            <input
                id="email"
                type="email"
                autoComplete="username"
                className={inputClass}
                aria-invalid={errors.email ? true : undefined}
                {...register('email')}
            />
            {errors.email && <span className="field-error">{errors.email.message}</span>}

            <label htmlFor="password" className={labelClass}>Password</label>
            <input
                id="password"
                type="password"
                autoComplete="current-password"
                className={inputClass}
                aria-invalid={errors.password ? true : undefined}
                {...register('password')}
            />
            {errors.password && <span className="field-error">{errors.password.message}</span>}

            <button 
                type="submit" 
                disabled={login.isPending}
                className="mt-5 cursor-pointer rounded-lg bg-blue-600 px-4 py-2.5 text-base font-medium text-white enabled:hover:bg-blue-700 disabled:cursor-not-allowed disabled:opacity-60">
            {login.isPending ? 'Memproses...' : 'Masuk'}
            </button>            
        </form>
    )
} 

