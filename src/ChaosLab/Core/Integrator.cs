namespace ChaosLab.Core
{
    /// <summary>
    /// Численные интеграторы. Никаких <c>stackalloc</c> и <c>unsafe</c>.
    /// </summary>
    public static class Integrator
    {
        /// <summary>
        /// Один шаг RK4 4-го порядка для потока.
        /// </summary>
        /// <param name="sys">Система (должна быть потоком, <c>IsDiscrete = false</c>).</param>
        /// <param name="s">Текущее состояние (изменяется in-place).</param>
        /// <param name="p">Параметры.</param>
        /// <param name="dt">Шаг по времени.</param>
        /// <param name="scr">Переиспользуемые буферы.</param>
        public static void Rk4Step(ChaoticSystem sys, double[] s, double[] p, double dt, Rk4Scratch scr)
        {
            int n = s.Length;
            sys.Derivatives(s, scr.K1, p);
            for (int i = 0; i < n; i++) scr.Tmp[i] = s[i] + 0.5 * dt * scr.K1[i];
            sys.Derivatives(scr.Tmp, scr.K2, p);
            for (int i = 0; i < n; i++) scr.Tmp[i] = s[i] + 0.5 * dt * scr.K2[i];
            sys.Derivatives(scr.Tmp, scr.K3, p);
            for (int i = 0; i < n; i++) scr.Tmp[i] = s[i] + dt * scr.K3[i];
            sys.Derivatives(scr.Tmp, scr.K4, p);
            for (int i = 0; i < n; i++)
                s[i] += dt / 6.0 * (scr.K1[i] + 2 * scr.K2[i] + 2 * scr.K3[i] + scr.K4[i]);
        }

        /// <summary>
        /// Один шаг эволюции системы: дискретное отображение или RK4-шаг потока.
        /// </summary>
        /// <param name="sys">Система.</param>
        /// <param name="s">Текущее состояние (изменяется in-place).</param>
        /// <param name="scratch">Рабочий буфер размером <see cref="ChaoticSystem.Dimension"/>
        /// (используется только для дискретных систем).</param>
        /// <param name="p">Параметры.</param>
        /// <param name="dt">Шаг интегрирования (только для потоков).</param>
        /// <param name="rk">RK4-буферы (только для потоков). Для отображений
        /// допускается передавать <c>null</c>.</param>
        public static void StepOnce(ChaoticSystem sys, double[] s, double[] scratch, double[] p, double dt, Rk4Scratch? rk)
        {
            if (sys.IsDiscrete)
            {
                sys.Next(s, scratch, p);
                Array.Copy(scratch, s, s.Length);
            }
            else
            {
                if (rk is null)
                {
                    throw new InvalidOperationException(
                        $"StepOnce: для потока '{sys.Name}' требуется Rk4Scratch, " +
                        $"но передан null. Убедитесь, что буферы инициализированы " +
                        $"под текущую систему (Dimension = {sys.Dimension}).");
                }
                Rk4Step(sys, s, p, dt, rk!);
            }
        }

        /// <summary>
        /// Один шаг RK4 для совместной системы (x, w), где ẋ = f(x) и ẇ = J(x)·w.
        /// </summary>
        /// <remarks>
        /// Используется вариационным методом расчёта показателей Ляпунова
        /// и картой Ляпунова. Вдвое дешевле двухтраекторного метода: одна
        /// траектория и одна JVP на стадию вместо двух траекторий.
        /// </remarks>
        /// <param name="sys">Система.</param>
        /// <param name="x">Траектория (изменяется in-place).</param>
        /// <param name="w">Вектор отклонения (изменяется in-place).</param>
        /// <param name="p">Параметры.</param>
        /// <param name="dt">Шаг интегрирования.</param>
        /// <param name="s">Переиспользуемые буферы.</param>
        public static void VariationalRk4Step(
            ChaoticSystem sys, double[] x, double[] w, double[] p, double dt,
            VariationalScratch s)
        {
            int n = x.Length;

            // k1
            sys.Derivatives(x, s.K1x, p);
            sys.JacobianVectorProduct(x, w, s.K1w, p);

            // k2
            for (int i = 0; i < n; i++) s.Tmp[i] = x[i] + 0.5 * dt * s.K1x[i];
            for (int i = 0; i < n; i++) s.TmpW[i] = w[i] + 0.5 * dt * s.K1w[i];
            sys.Derivatives(s.Tmp, s.K2x, p);
            sys.JacobianVectorProduct(s.Tmp, s.TmpW, s.K2w, p);

            // k3
            for (int i = 0; i < n; i++) s.Tmp[i] = x[i] + 0.5 * dt * s.K2x[i];
            for (int i = 0; i < n; i++) s.TmpW[i] = w[i] + 0.5 * dt * s.K2w[i];
            sys.Derivatives(s.Tmp, s.K3x, p);
            sys.JacobianVectorProduct(s.Tmp, s.TmpW, s.K3w, p);

            // k4
            for (int i = 0; i < n; i++) s.Tmp[i] = x[i] + dt * s.K3x[i];
            for (int i = 0; i < n; i++) s.TmpW[i] = w[i] + dt * s.K3w[i];
            sys.Derivatives(s.Tmp, s.K4x, p);
            sys.JacobianVectorProduct(s.Tmp, s.TmpW, s.K4w, p);

            // Update
            for (int i = 0; i < n; i++)
                x[i] += dt / 6.0 * (s.K1x[i] + 2 * s.K2x[i] + 2 * s.K3x[i] + s.K4x[i]);
            for (int i = 0; i < n; i++)
                w[i] += dt / 6.0 * (s.K1w[i] + 2 * s.K2w[i] + 2 * s.K3w[i] + s.K4w[i]);
        }

        /// <summary>
        /// Один шаг адаптивного интегратора Dormand-Prince RK45 с контролем
        /// локальной ошибки.
        /// </summary>
        /// <param name="sys">Система (поток, не отображение).</param>
        /// <param name="s">Текущее состояние (изменяется in-place).</param>
        /// <param name="p">Параметры.</param>
        /// <param name="dt">
        /// Рекомендуемый шаг. После вызова содержит скорректированный шаг,
        /// вычисленный по оценке ошибки. Передавайте <c>ref dt</c> между вызовами
        /// для адаптации.
        /// </param>
        /// <param name="tolerance">
        /// Допустимая относительная ошибка на шаг. Типичные значения:
        /// <c>1e-6</c> — высокая точность, <c>1e-4</c> — быстро, <c>1e-3</c> — для
        /// визуализации.
        /// </param>
        /// <param name="scr">Переиспользуемые буферы.</param>
        /// <returns>
        /// <c>true</c> — шаг принят; <c>false</c> — шаг отклонён (состояние не
        /// изменено, <paramref name="dt"/> уменьшен, повторите вызов).
        /// </returns>
        /// <remarks>
        /// <para>Метод Dormand-Prince RK45 — 5-й порядок точности с встроенной
        /// оценкой ошибки через 4-й порядок. Отношение ошибок даёт адаптивный
        /// шаг: <c>dt_new = dt · (tol/err)^0.2</c>.</para>
        /// <para>При отклонённом шаге состояние не меняется — вызывающий должен
        /// повторить с уменьшенным <paramref name="dt"/>.</para>
        /// </remarks>
        public static bool Rk45Step(ChaoticSystem sys, double[] s, double[] p,
            ref double dt, double tolerance, Rk45Scratch scr)
        {
            // Коэффициенты Dormand-Prince (Butcher tableau)
            // c₂..c₇ — узлы; a_i,j — веса; b_i — веса решения 5-го порядка;
            // b*_i — веса 4-го порядка (для оценки ошибки)
            int n = s.Length;

            sys.Derivatives(s, scr.K1, p);

            for (int i = 0; i < n; i++) scr.Tmp[i] = s[i] + dt * (1.0 / 5.0) * scr.K1[i];
            sys.Derivatives(scr.Tmp, scr.K2, p);

            for (int i = 0; i < n; i++)
                scr.Tmp[i] = s[i] + dt * ((3.0 / 40.0) * scr.K1[i] + (9.0 / 40.0) * scr.K2[i]);
            sys.Derivatives(scr.Tmp, scr.K3, p);

            for (int i = 0; i < n; i++)
                scr.Tmp[i] = s[i] + dt * ((44.0 / 45.0) * scr.K1[i] - (56.0 / 15.0) * scr.K2[i]
                                         + (32.0 / 9.0) * scr.K3[i]);
            sys.Derivatives(scr.Tmp, scr.K4, p);

            for (int i = 0; i < n; i++)
                scr.Tmp[i] = s[i] + dt * ((19372.0 / 6561.0) * scr.K1[i]
                                         - (25360.0 / 2187.0) * scr.K2[i]
                                         + (64448.0 / 6561.0) * scr.K3[i]
                                         - (212.0 / 729.0) * scr.K4[i]);
            sys.Derivatives(scr.Tmp, scr.K5, p);

            for (int i = 0; i < n; i++)
                scr.Tmp[i] = s[i] + dt * ((9017.0 / 3168.0) * scr.K1[i]
                                         - (355.0 / 33.0) * scr.K2[i]
                                         + (46732.0 / 5247.0) * scr.K3[i]
                                         + (49.0 / 176.0) * scr.K4[i]
                                         - (5103.0 / 18656.0) * scr.K5[i]);
            sys.Derivatives(scr.Tmp, scr.K6, p);

            for (int i = 0; i < n; i++)
                scr.Tmp[i] = s[i] + dt * ((35.0 / 384.0) * scr.K1[i]
                                         + (500.0 / 1113.0) * scr.K3[i]
                                         + (125.0 / 192.0) * scr.K4[i]
                                         - (2187.0 / 6784.0) * scr.K5[i]
                                         + (11.0 / 84.0) * scr.K6[i]);
            sys.Derivatives(scr.Tmp, scr.K7, p);

            // Оценка ошибки: разность между 5-м и 4-м порядками
            double maxErr = 0;
            for (int i = 0; i < n; i++)
            {
                double sol5 = s[i] + dt * ((35.0 / 384.0) * scr.K1[i]
                                         + (500.0 / 1113.0) * scr.K3[i]
                                         + (125.0 / 192.0) * scr.K4[i]
                                         - (2187.0 / 6784.0) * scr.K5[i]
                                         + (11.0 / 84.0) * scr.K6[i]);

                double sol4 = s[i] + dt * ((5179.0 / 57600.0) * scr.K1[i]
                                         + (7571.0 / 16695.0) * scr.K3[i]
                                         + (393.0 / 640.0) * scr.K4[i]
                                         - (92097.0 / 339200.0) * scr.K5[i]
                                         + (187.0 / 2100.0) * scr.K6[i]
                                         + (1.0 / 40.0) * scr.K7[i]);

                scr.Err[i] = sol5 - sol4;

                double scale = 1e-8 + tolerance * Math.Abs(sol5);
                double ratio = Math.Abs(scr.Err[i]) / scale;
                if (ratio > maxErr) maxErr = ratio;
            }

            // Ошибка превышает допуск → отклоняем шаг
            if (maxErr > 1.0)
            {
                // Уменьшаем dt: dt_new = dt · safety · (1/err)^0.2
                const double safety = 0.9;
                double factor = safety * Math.Pow(1.0 / maxErr, 0.2);
                if (factor < 0.1) factor = 0.1;
                dt *= factor;
                return false;
            }

            // Принимаем шаг: обновляем состояние решением 5-го порядка
            for (int i = 0; i < n; i++)
                s[i] += dt * ((35.0 / 384.0) * scr.K1[i]
                            + (500.0 / 1113.0) * scr.K3[i]
                            + (125.0 / 192.0) * scr.K4[i]
                            - (2187.0 / 6784.0) * scr.K5[i]
                            + (11.0 / 84.0) * scr.K6[i]);

            // Адаптируем dt на следующий шаг (с ограничением роста)
            const double growLimit = 5.0;
            const double shrinkLimit = 0.2;
            double adapt = 0.9 * Math.Pow(1.0 / (maxErr + 1e-16), 0.2);
            if (adapt > growLimit) adapt = growLimit;
            if (adapt < shrinkLimit) adapt = shrinkLimit;
            dt *= adapt;

            return true;
        }

        /// <summary>
        /// Один шаг симплектического интегратора Verlet (leapfrog) для
        /// гамильтоновых систем вида <c>H = T(p) + V(q)</c>.
        /// </summary>
        /// <param name="sys">Система (поток, гамильтонова, чётная размерность).</param>
        /// <param name="s">
        /// Состояние размера 2·N, где первые N компонент — координаты q,
        /// последние N — импульсы p.
        /// </param>
        /// <param name="p">Параметры системы.</param>
        /// <param name="dt">Шаг по времени.</param>
        /// <param name="scr">Переиспользуемые буферы (минимум 2·N элементов).</param>
        /// <remarks>
        /// <para>Verlet сохраняет **симплектическую структуру** и **энергию**:
        /// фазовый объём не дрейфует, ошибка в H не накапливается (в отличие
        /// от RK4).</para>
        /// <para><b>Схема:</b></para>
        /// <code>
        /// p_{n+1/2} = p_n − (dt/2)·∇V(q_n)
        /// q_{n+1}   = q_n + dt·p_{n+1/2}
        /// p_{n+1}   = p_{n+1/2} − (dt/2)·∇V(q_{n+1})
        /// </code>
        /// <para>Общая сложность — 1 вычисление ∇V на шаг (в 4 раза дешевле RK4).</para>
        /// <para><b>Требование:</b> система должна реализовать
        /// <see cref="ISymplecticSystem"/>. Для остальных вызывайте RK4.</para>
        /// </remarks>
        public static void VerletStep(ISymplecticSystem sys, double[] s,
            double[] p, double dt, VerletScratch scr)
        {
            int N = sys.HalfDimension;

            // p_{n+1/2} = p_n − (dt/2)·∇V(q_n)
            sys.GradientPotential(s.AsSpan(0, N), scr.GradV, p);
            for (int i = 0; i < N; i++)
                scr.HalfP[i] = s[N + i] - 0.5 * dt * scr.GradV[i];

            // q_{n+1} = q_n + dt·p_{n+1/2}
            for (int i = 0; i < N; i++)
                s[i] += dt * scr.HalfP[i];

            // p_{n+1} = p_{n+1/2} − (dt/2)·∇V(q_{n+1})
            sys.GradientPotential(s.AsSpan(0, N), scr.GradV, p);
            for (int i = 0; i < N; i++)
                s[N + i] = scr.HalfP[i] - 0.5 * dt * scr.GradV[i];
        }
    }
}
