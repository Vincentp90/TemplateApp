'use client';

import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useState } from 'react';
import { router } from '../router';
import { queryClient } from '../queryClient';
import WlButton from './tiny/wlButton';

const APIURL = import.meta.env.VITE_API_URL;//TODO use axios api object instead of fetch?? 

const schema = z.object({
    email: z.email('Invalid email'),
    password: z.string().min(6, 'Password must be at least 6 chars'),
})

type FormData = z.infer<typeof schema>

const isDev = import.meta.env.MODE === "development";

export default function LoginForm() {
    const [action, setAction] = useState<string>("login");
    const [isRegistered, setIsRegistered] = useState<boolean>(false);
    const { register, handleSubmit, formState: { errors, isSubmitting }, setValue } = useForm<FormData>({
        resolver: zodResolver(schema),
        mode: 'onBlur',
    })

    const onSubmit = async (data: FormData) => {
        const res = await fetch(`${APIURL}/auth/${action}`, {
            method: "POST",
            credentials: "include",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ username: data.email, password: data.password })
        });
        console.log(res);
        if (!res.ok) throw new Error("Login failed");// TODO show nice error in UI, hook form has something for this?
        if (action === "login") {
            router.navigate({ to: "/app" });
            queryClient.clear();
        }
        else if (action === "register") {
            setIsRegistered(true);//TODO show in nicer way or make separate register screen
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

    return (
        <form
            onSubmit={handleSubmit(onSubmit)}
            className="flex flex-col gap-3 max-w-sm mx-auto p-4"
        >
            <div><span>APIURL: {import.meta.env.VITE_API_URL}</span></div>
            {isDev && (
                <>
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
        </form>
    )
}