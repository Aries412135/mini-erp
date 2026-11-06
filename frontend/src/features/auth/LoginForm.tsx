import { useForm } from "react-hook-form";
import { type LoginFormValues, loginSchema } from "./loginSchema";
import { zodResolver } from "@hookform/resolvers/zod";
import { useLogin } from "./useLogin";
import { getErrorMessage } from "../../utils/getErrorMessage";

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
            className="login-form"
            noValidate
            onSubmit={handleSubmit((values) => login.mutate(values))}
        >
            <h1>Mini ERP</h1>
            <p className="subtitle">Masuk untuk melanjutkan</p>

            {login.isError && (
                <div className="alert" role="alert">
                    {getErrorMessage(login.error)}
                </div>
            )}

            <label htmlFor="email">Email</label>
            <input
                id="email"
                type="email"
                autoComplete="username"
                aria-invalid={errors.email ? true : undefined}
                {...register('email')}
            />
            {errors.email && <span className="field-error">{errors.email.message}</span>}

            <label htmlFor="password">Password</label>
            <input
                id="password"
                type="password"
                autoComplete="current-password"
                aria-invalid={errors.password ? true : undefined}
                {...register('password')}
            />
            {errors.password && <span className="field-error">{errors.password.message}</span>}

            <button type="submit" disabled={login.isPending}>
            {login.isPending ? 'Memproses...' : 'Masuk'}
            </button>            
        </form>
    )
} 

