'use client';

import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useState } from 'react';
import { router } from '../router';
import { queryClient } from '../queryClient';
import WlButton from './tiny/wlButton';
import { api } from '../api';
import type { AxiosError } from 'axios';

const schema = z.object({
    email: z.email('Invalid email'),
    password: z.string().min(6, 'Password must be at least 6 chars'),
})

type FormData = z.infer<typeof schema>

const isDev = import.meta.env.MODE === "development";

export default function LoginForm() {
    const [action, setAction] = useState<string>("login");
    const [isRegistered, setIsRegistered] = useState<boolean>(false);
    const { register, handleSubmit, formState: { errors, isSubmitting }, setValue, setError } = useForm<FormData>({
        resolver: zodResolver(schema),
        mode: 'onBlur',
    })

    const onSubmit = async (data: FormData) => {
        try {
            await api.post(`/auth/${action}`, { username: data.email, password: data.password });
            if (action === "login") {
                router.navigate({ to: "/app" });
                queryClient.clear();
            }
            else if (action === "register") {
                setIsRegistered(true);//TODO show in nicer way or make separate register screen
            }
        } catch (err: unknown) {
            const axiosError = err as AxiosError;
            if (axiosError.status === 401) {
                setError("root", {
                    type: "manual",
                    message: "Incorrect email or password"
                });
            } else {
                setError("root", {
                    type: "manual",
                    message: "Unknown error"
                });
                console.error('Other error:', err);
            }
        }
    }

    const fillDevCreds = () => {
        setValue("email", "dev@example.com");
        setValue("password", "password123");
    };
    const fillDevCreds2 = () => {
        setValue("email", "dev2@example2.com");
        setValue("password", "password123");
    };

    const apiUrl = import.meta.env.VITE_API_URL;

    return (
        <form
            onSubmit={handleSubmit(onSubmit)}
            className="flex flex-col gap-3 max-w-sm mx-auto p-4"
        >
            {isDev && (
                <>
                    <div><span>APIURL: <a href={`${apiUrl}/swagger/index.html`}>{apiUrl}/swagger/index.html</a></span></div>
                    <WlButton onClick={fillDevCreds} isPrimary={true}>
                        Login with test acc 1
                    </WlButton>
                    <WlButton onClick={fillDevCreds2} isPrimary={true}>
                        Login with test acc 2
                    </WlButton>
                </>
            )}
            <label className="flex flex-col">
                <span>Email</span>
                <input
                    {...register('email')}
                    type="email"
                    className="border rounded p-2"
                />
                {errors.email && (
                    <p className="text-red-500 text-sm">{errors.email.message}</p>
                )}
            </label>

            <label className="flex flex-col">
                <span>Password</span>
                <input
                    {...register('password')}
                    type="password"
                    className="border rounded p-2"
                />
                {errors.password && (
                    <p className="text-red-500 text-sm">{errors.password.message}</p>
                )}
            </label>

            <WlButton
                type="submit"
                disabled={isSubmitting}
                onClick={() => setAction("login")}
                isPrimary={true}
            >
                {isSubmitting ? 'Submitting...' : 'Login'}
            </WlButton>
            <WlButton
                type="submit"
                disabled={isSubmitting}
                onClick={() => setAction("register")}
                isPrimary={false}
            >
                {isSubmitting ? 'Submitting...' : 'Register'}
            </WlButton>
            {isRegistered && <div>Registered, now you can click login</div>}
            {errors.root && <p style={{ color: "red" }}>{errors.root.message}</p>}
        </form>
    )
}