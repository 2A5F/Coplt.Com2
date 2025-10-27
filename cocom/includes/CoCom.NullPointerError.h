#pragma once
#ifndef COPLT_COM_NULL_POINTER_ERROR_H
#define COPLT_COM_NULL_POINTER_ERROR_H

#include <exception>
#include <stacktrace>
#include <utility>
#include <format>

namespace Coplt
{
    class NullPointerError : public std::exception
    {
        mutable std::string m_message;
        std::stacktrace m_stacktrace;

    public:
        explicit NullPointerError(
            char const* const message,
            std::stacktrace stacktrace = std::stacktrace::current()
        ) : std::exception(message), m_stacktrace(std::move(stacktrace))
        {
        }

        explicit NullPointerError(
            std::stacktrace stacktrace = std::stacktrace::current()
        ) : m_stacktrace(std::move(stacktrace))
        {
        }

        char const* what() const override
        {
            if (!m_message.empty()) return m_message.c_str();
            m_message = build_message();
            return m_message.c_str();
        }

        std::string build_message() const
        {
            return std::format("{}\n{}", std::exception::what(), m_stacktrace);
        }

        const std::stacktrace& stacktrace() const
        {
            return m_stacktrace;
        }
    };
}

#endif //COPLT_COM_NULL_POINTER_ERROR_H
