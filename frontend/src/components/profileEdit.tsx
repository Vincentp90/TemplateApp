'use client';

import { useSuspenseQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { api } from "../api";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { zodResolver } from "@hookform/resolvers/zod";
import { router } from "../router";

type UserDetails = {
    rowVersion: number;
    email: string;
    firstName: string | null;
    lastName: string | null;
    country: string | null;
    city: string | null;
    address: string | null;
};

// Zod schema for validation
const userSchema = z.object({
    rowVersion: z.number(),
    email: z.email(),
    firstName: z.string().nullable(),
    lastName: z.string().nullable(),
    country: z.string().nullable(),
    city: z.string().nullable(),
    address: z.string().nullable(),
});

export default function ProfileEdit() {
    const queryClient = useQueryClient();

    const { data: userDetails } = useSuspenseQuery<UserDetails>({
        queryKey: ['userDetails'],
        queryFn: async () => {
            const res = await api.get("/user");
            return res.data;
        },
    });

    const { register, handleSubmit, formState: { errors } } = useForm<UserDetails>({
        resolver: zodResolver(userSchema),
        defaultValues: userDetails,
    });

    const mutation = useMutation({
        mutationFn: (data: UserDetails) => api.post("/users/me", data),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey:['userDetails'] });
            router.navigate({ to: "/app/profile" });
        },
    });

    const onSubmit = (data: UserDetails) => mutation.mutate(data);

    return (
        <div className="max-w-3xl mx-auto p-6">
            <h2 className="text-2xl font-semibold mb-6">Edit Profile</h2>
            <form className="grid grid-cols-1 sm:grid-cols-2 gap-6" onSubmit={handleSubmit(onSubmit)}>
                <InputField label="First Name" {...register("firstName")} error={errors.firstName?.message} />
                <InputField label="Last Name" {...register("lastName")} error={errors.lastName?.message} />
                <Field label="Email" value={userDetails.email} />
                <InputField label="Country" {...register("country")} error={errors.country?.message} />
                <InputField label="City" {...register("city")} error={errors.city?.message} />
                <InputField label="Address" {...register("address")} error={errors.address?.message} />
                <div className="sm:col-span-2 flex justify-end mt-4">
                    <button
                        type="submit"
                        className="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700 disabled:opacity-50"
                        disabled={mutation.isPending}
                    >
                        {mutation.isPending ? "Saving..." : "Save"}
                    </button>
                </div>
            </form>
        </div>
    );
}

function InputField({ label, error, ...props }: { label: string; error?: string } & React.ComponentProps<'input'>) {
    return (
        <div className="flex flex-col bg-white shadow rounded-xl p-4 border border-gray-200">
            <label className="text-sm font-medium text-gray-600">{label}</label>
            <input
                className={`mt-1 border rounded px-3 py-2 text-gray-900 focus:outline-none focus:ring-2 focus:ring-blue-500 ${error ? "border-red-500" : ""}`}
                {...props}
            />
            {error && <span className="text-red-500 text-sm mt-1">{error}</span>}
        </div>
    );
}

function Field({ label, value }: { label: string; value: string | null }) {
    return (
        <div className="flex flex-col bg-white shadow rounded-xl p-4 border border-gray-200">
            <span className="text-sm font-medium text-gray-600">{label}</span>
            <span className="mt-1 text-gray-900">{value ?? ""}</span>
        </div>
    );
}