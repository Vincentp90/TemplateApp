'use client';

import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useState } from 'react';
import { router } from '../router';
import { queryClient } from '../queryClient';

const APIURL = "http://localhost:5186";//TODO use axios api object instead of fetch?? 

const schema = z.object({
    email: z.email('Invalid email'),
    password: z.string().min(6, 'Password must be at least 6 chars'),
})

type FormData = z.infer<typeof schema>

const isDev = import.meta.env.MODE === "development";

export default function LoginForm() {
    const [action, setAction] = useState<string>("login");
    const { register, handleSubmit, formState: { errors, isSubmitting }, setValue} = useForm<FormData>({
        resolver: zodResolver(schema),
        mode: 'onBlur',
    })

    const onSubmit = async (data: FormData) => {
        const res = await fetch(`${APIURL}/auth/${action}`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ username: data.email, password: data.password })
        });
        if (!res.ok) throw new Error("Login failed");// TODO show nice error in UI
        const { token } = await res.json();
        localStorage.setItem("token", token);
        if(action === "login")
        {
            router.navigate({ to: "/app" });
            queryClient.clear();
        }        
    }

    const fillDevCreds = () => {
        setValue("email", "dev@example.com");
        setValue("password", "password123");
    };

    return (
        <form
            onSubmit={handleSubmit(onSubmit)}
            className="flex flex-col gap-3 max-w-sm mx-auto p-4"
        >
            {isDev && (
                <button onClick={fillDevCreds} className="mt-2 bg-gray-500 text-white p-2 w-full">
                    I'm a developer
                </button>
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

            <button
                type="submit"
                disabled={isSubmitting}
                className="bg-blue-600 text-white p-2 rounded"
                onClick={() => setAction("login")}
            >
                {isSubmitting ? 'Submitting...' : 'Login'}
            </button>
            <button
                type="submit"
                disabled={isSubmitting}
                className="bg-blue-600 text-white p-2 rounded"
                onClick={() => setAction("register")}
            >
                {isSubmitting ? 'Submitting...' : 'Register'}
            </button>
        </form>
    )
}